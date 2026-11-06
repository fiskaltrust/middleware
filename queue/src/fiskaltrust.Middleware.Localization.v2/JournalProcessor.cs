using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.Base.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IJournalProcessor
{
    (ContentType contentType, IAsyncEnumerable<byte[]> result) ProcessAsync(JournalRequest request);
}


public class JournalProcessor
{
    private readonly AsyncLazy<IReadOnlyConfigurationRepository> _configurationRepository;
    private readonly AsyncLazy<IMiddlewareRepository<ftQueueItem>> _queueItemRepository;
    private readonly AsyncLazy<IMiddlewareRepository<ftReceiptJournal>> _receiptJournalRepository;
    private readonly AsyncLazy<IMiddlewareRepository<ftActionJournal>> _actionJournalRepository;
    private readonly IJournalProcessor _marketSpecificJournalProcessor;
    private readonly ILogger<JournalProcessor> _logger;
    private readonly Dictionary<string, object> _configuration;

    public JournalProcessor(
        IStorageProvider storageProvider,
        IJournalProcessor marketSpecificJournalProcessor,
        Dictionary<string, object> configuration,
        ILogger<JournalProcessor> logger)
    {
        _configurationRepository = storageProvider.CreateConfigurationRepository().Cast<IConfigurationRepository, IReadOnlyConfigurationRepository>();
        _queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository().Cast<IMiddlewareQueueItemRepository, IMiddlewareRepository<ftQueueItem>>();
        _receiptJournalRepository = storageProvider.CreateMiddlewareReceiptJournalRepository().Cast<IMiddlewareReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>>();
        _actionJournalRepository = storageProvider.CreateMiddlewareActionJournalRepository().Cast<IMiddlewareActionJournalRepository, IMiddlewareRepository<ftActionJournal>>();
        _marketSpecificJournalProcessor = marketSpecificJournalProcessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(ContentType, PipeReader)> ProcessAsync(JournalRequest request)
    {
        ContentType contentType;
        IAsyncEnumerable<byte[]> response;

        try
        {
            if (request.ftJournalType.Country() is not null)
            {
                (contentType, response) = _marketSpecificJournalProcessor.ProcessAsync(request);
            }
            else
            {
                contentType = new ContentType(MediaTypeNames.Application.Json) { CharSet = Encoding.UTF8.WebName };

                response = request.ftJournalType switch
                {
                    JournalType.ActionJournal => GetFromEntitiesAsync(await _actionJournalRepository, request),
                    JournalType.ReceiptJournal => GetFromEntitiesAsync(await _receiptJournalRepository, request),
                    JournalType.QueueItem => GetFromEntitiesAsync(await _queueItemRepository, request),
                    JournalType.Configuration => new[] { Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(await GetConfigurationAsync())) }.ToAsyncEnumerable(),
                    _ => new[] {Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Assembly = ThisAssembly.AssemblyName,
                        ThisAssembly.AssemblyVersion
                    }))}.ToAsyncEnumerable()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
            throw;
        }

        var pipe = new Pipe();

        // For PT market, stream directly to pipe without using a temp file
        if (request.ftJournalType.Country() == "PT")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var journal in response)
                    {
                        await pipe.Writer.WriteAsync(journal);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while processing the Journal request for PT market.");
                    await pipe.Writer.CompleteAsync(ex);
                    throw;
                }
                finally
                {
                    await pipe.Writer.CompleteAsync();
                }
            });

            return (contentType, pipe.Reader);
        }

        // For other markets, use temp file storage
        var tempFile = Path.GetTempFileName();
        try
        {
            using var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Write);
            await foreach (var journal in response)
            {
                await fileStream.WriteAsync(journal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
            File.Delete(tempFile);
            throw;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
                await fileStream.CopyToAsync(pipe.Writer.AsStream());
            }
            finally
            {
                await pipe.Writer.CompleteAsync();
                File.Delete(tempFile);
            }
        });

        return (contentType, pipe.Reader);
    }

    private async Task<object> GetConfigurationAsync()
    {
        var configurationRepository = await _configurationRepository.Value;
        return new
        {
            Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
            typeof(JournalProcessor).Assembly.GetName().Version,
            CashBoxList = await configurationRepository.GetCashBoxListAsync().ConfigureAwait(false),
            QueueList = await configurationRepository.GetQueueListAsync().ConfigureAwait(false),
            QueueATList = await configurationRepository.GetQueueATListAsync().ConfigureAwait(false),
            QueueBEList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetQueueBEListAsync, "init_ftQueueBE").ConfigureAwait(false),
            QueueDEList = await configurationRepository.GetQueueDEListAsync().ConfigureAwait(false),
            QueueESList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetQueueESListAsync, "init_ftQueueES").ConfigureAwait(false),
            QueueEUList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetQueueEUListAsync, "init_ftQueueEU").ConfigureAwait(false),
            QueueFRList = await configurationRepository.GetQueueFRListAsync().ConfigureAwait(false),
            QueueGRList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetQueueGRListAsync, "init_ftQueueGR").ConfigureAwait(false),
            QueueITList = await configurationRepository.GetQueueITListAsync().ConfigureAwait(false),
            QueueMEList = await configurationRepository.GetQueueMEListAsync().ConfigureAwait(false),
            QueuePLList = await configurationRepository.GetQueuePLListAsync().ConfigureAwait(false),
            QueuePTList = GetConfigurationFromDictionary<ftQueuePT>("init_ftQueuePT"),
            SignaturCreationUnitATList = await configurationRepository.GetSignaturCreationUnitATListAsync().ConfigureAwait(false),
            SignaturCreationUnitBEList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetSignaturCreationUnitBEListAsync, "init_ftSignaturCreationUnitBE").ConfigureAwait(false),
            SignaturCreationUnitDEList = await configurationRepository.GetSignaturCreationUnitDEListAsync().ConfigureAwait(false),
            SignaturCreationUnitESList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetSignaturCreationUnitESListAsync, "init_ftSignaturCreationUnitES").ConfigureAwait(false),
            SignaturCreationUnitFRList = await configurationRepository.GetSignaturCreationUnitFRListAsync().ConfigureAwait(false),
            SignaturCreationUnitGRList = await GetStoredOrInitialConfigurationAsync(configurationRepository.GetSignaturCreationUnitGRListAsync, "init_ftSignaturCreationUnitGR").ConfigureAwait(false),
            SignaturCreationUnitITList = await configurationRepository.GetSignaturCreationUnitITListAsync().ConfigureAwait(false),
            SignaturCreationUnitMEList = await configurationRepository.GetSignaturCreationUnitMEListAsync().ConfigureAwait(false),
            SignaturCreationUnitPLList = await configurationRepository.GetSignaturCreationUnitPLListAsync().ConfigureAwait(false),
            SignaturCreationUnitPTList = GetConfigurationFromDictionary<ftSignaturCreationUnitPT>("init_ftSignaturCreationUnitPT"),
        };
    }

    private async Task<IEnumerable<T>> GetStoredOrInitialConfigurationAsync<T>(Func<Task<IEnumerable<T>>> getStoredConfiguration, string key)
    {
        try
        {
            return await getStoredConfiguration().ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            // Not every storage backend implements this getter; the startup configuration is all that is left there.
            return GetConfigurationFromDictionary<T>(key);
        }
    }

    private List<T> GetConfigurationFromDictionary<T>(string key)
    {
        try
        {
            if (_configuration.ContainsKey(key))
            {
                return JsonConvert.DeserializeObject<List<T>>(_configuration[key]!.ToString()!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
        }
        return [];
    }

    private async IAsyncEnumerable<byte[]> GetFromEntitiesAsync<T>(IMiddlewareRepository<T> repository, JournalRequest request)
    {
        IAsyncEnumerable<T> result;
        if (request.To < 0)
        {
            result = repository.GetEntriesOnOrAfterTimeStampAsync(request.From, take: (int) -request.To);
        }
        else if (request.To == 0)
        {
            result = repository.GetEntriesOnOrAfterTimeStampAsync(request.From);
        }
        else
        {
            result = repository.GetByTimeStampRangeAsync(request.From, request.To);
        }

        yield return Encoding.UTF8.GetBytes("[");
        await foreach (var (i, journal) in result.Select((j, i) => (i, j)))
        {
            if (i != 0)
            {
                yield return Encoding.UTF8.GetBytes(",");
            }
            yield return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(journal));
        }
        yield return Encoding.UTF8.GetBytes("]");
    }
}
