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
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IJournalProcessor
{
    Task<(ContentType contentType, PipeReader reader)> ProcessAsync(JournalRequest request);
}

public class JournalProcessor : IJournalProcessor
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
        PipeReader response;

        try
        {
            if (request.ftJournalType.Case() != 0)
            {
                (contentType, response) = await _marketSpecificJournalProcessor.ProcessAsync(request);
            }
            else
            {
                contentType = new ContentType(MediaTypeNames.Application.Json) { CharSet = Encoding.UTF8.WebName };

                response = request.ftJournalType switch
                {
                    JournalType.ActionJournal => ToJournalResponse(GetEntitiesAsync(await _actionJournalRepository, request)),
                    JournalType.ReceiptJournal => ToJournalResponse(GetEntitiesAsync(await _receiptJournalRepository, request)),
                    JournalType.QueueItem => ToJournalResponse(GetEntitiesAsync(await _queueItemRepository, request)),
                    JournalType.Configuration => PipeReader.Create(new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(await GetConfigurationAsync())))),
                    _ => PipeReader.Create(new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
                        typeof(JournalProcessor).Assembly.GetName().Version
                    }))))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
            throw;
        }

        return (contentType, response);
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
            QueueDEList = await configurationRepository.GetQueueDEListAsync().ConfigureAwait(false),
            QueueESList = GetConfigurationFromDictionary<ftQueueES>("init_ftQueueES"),
            QueueEUList = GetConfigurationFromDictionary<ftQueueES>("init_ftQueueEU"),
            QueueFRList = await configurationRepository.GetQueueFRListAsync().ConfigureAwait(false),
            QueueGRList = GetConfigurationFromDictionary<ftQueueGR>("init_ftQueueGR"),
            QueueITList = await configurationRepository.GetQueueITListAsync().ConfigureAwait(false),
            QueueMEList = await configurationRepository.GetQueueMEListAsync().ConfigureAwait(false),
            QueuePTList = GetConfigurationFromDictionary<ftQueuePT>("init_ftQueuePT"),
            SignaturCreationUnitATList = await configurationRepository.GetSignaturCreationUnitATListAsync().ConfigureAwait(false),
            SignaturCreationUnitDEList = await configurationRepository.GetSignaturCreationUnitDEListAsync().ConfigureAwait(false),
            SignaturCreationUnitESList = GetConfigurationFromDictionary<ftSignaturCreationUnitES>("init_ftSignaturCreationUnitES"),
            SignaturCreationUnitFRList = await configurationRepository.GetSignaturCreationUnitFRListAsync().ConfigureAwait(false),
            SignaturCreationUnitGRList = GetConfigurationFromDictionary<ftSignaturCreationUnitGR>("init_ftSignaturCreationUnitGR"),
            SignaturCreationUnitITList = await configurationRepository.GetSignaturCreationUnitITListAsync().ConfigureAwait(false),
            SignaturCreationUnitMEList = await configurationRepository.GetSignaturCreationUnitMEListAsync().ConfigureAwait(false),
            SignaturCreationUnitPTList = GetConfigurationFromDictionary<ftSignaturCreationUnitPT>("init_ftSignaturCreationUnitPT"),
        };
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

    private PipeReader ToJournalResponse<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        Pipe response = new();

        Task.Run(async () =>
        {
            try
            {
                await foreach (var journal in asyncEnumerable)
                {
                    try
                    {
                        await response.Writer.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(journal)));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while writing journal data to the response\n.");
                    }
                }
            }
            finally
            {
                await response.Writer.CompleteAsync();
            }
        });

        return response.Reader;
    }

    private IAsyncEnumerable<T> GetEntitiesAsync<T>(IMiddlewareRepository<T> repository, JournalRequest request)
    {
        if (request.Take.HasValue)
        {
            return repository.GetEntriesOnOrAfterTimeStampAsync(request.From?.Ticks ?? 0, take: (int) -request.Take.Value);
        }
        else if (request.To is null)
        {
            return repository.GetEntriesOnOrAfterTimeStampAsync(request.From?.Ticks ?? 0);
        }
        else
        {
            return repository.GetByTimeStampRangeAsync(request.From?.Ticks ?? 0, request.To?.Ticks ?? 0);
        }
    }
}
