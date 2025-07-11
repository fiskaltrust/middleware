using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Extensions;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IJournalProcessor
{
    IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request);
}

public class JournalProcessor : IJournalProcessor
{
    private readonly Lazy<Task<IReadOnlyConfigurationRepository>> _configurationRepository;
    private readonly Lazy<Task<IMiddlewareRepository<ftQueueItem>>> _queueItemRepository;
    private readonly Lazy<Task<IMiddlewareRepository<ftReceiptJournal>>> _receiptJournalRepository;
    private readonly Lazy<Task<IMiddlewareRepository<ftActionJournal>>> _actionJournalRepository;
    private readonly IJournalProcessor _marketSpecificJournalProcessor;
    private readonly ILogger<JournalProcessor> _logger;
    private readonly Dictionary<string, object> _configuration;

    public JournalProcessor(
        IStorageProvider storageProvider,
        IJournalProcessor marketSpecificJournalProcessor,
        Dictionary<string, object> configuration,
        ILogger<JournalProcessor> logger)
    {
        _configurationRepository = storageProvider.ConfigurationRepository.Cast<IConfigurationRepository, IReadOnlyConfigurationRepository>();
        _queueItemRepository = storageProvider.MiddlewareQueueItemRepository.Cast<IMiddlewareQueueItemRepository, IMiddlewareRepository<ftQueueItem>>();
        _receiptJournalRepository = storageProvider.MiddlewareReceiptJournalRepository.Cast<IMiddlewareReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>>();
        _actionJournalRepository = storageProvider.MiddlewareActionJournalRepository.Cast<IMiddlewareActionJournalRepository, IMiddlewareRepository<ftActionJournal>>();
        _marketSpecificJournalProcessor = marketSpecificJournalProcessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        IAsyncEnumerable<JournalResponse> responses;

        try
        {
            if ((0xFFFF000000000000 & (ulong) request.ftJournalType) != 0)
            {
                responses = _marketSpecificJournalProcessor.ProcessAsync(request);
            }
            else
            {
                responses = request.ftJournalType switch
                {
                    (long) JournalTypes.ActionJournal => ToJournalResponseAsync(GetEntitiesAsync(await _actionJournalRepository.Value, request), request.MaxChunkSize),
                    (long) JournalTypes.ReceiptJournal => ToJournalResponseAsync(GetEntitiesAsync(await _receiptJournalRepository.Value, request), request.MaxChunkSize),
                    (long) JournalTypes.QueueItem => ToJournalResponseAsync(GetEntitiesAsync(await _queueItemRepository.Value, request), request.MaxChunkSize),
                    (long) JournalTypes.Configuration => new List<JournalResponse> {
                    new JournalResponse
                    {
                        Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(GetConfiguration().Result)).ToList()
                    }
                }.ToAsyncEnumerable(),
                    _ => new List<JournalResponse> {
                        new JournalResponse
                        {
                            Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                                {
                                    Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
                                    typeof(JournalProcessor).Assembly.GetName().Version
                                }
                            )).ToList()
                        }
                }.ToAsyncEnumerable()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
            throw;
        }

        await foreach (var response in responses)
        {
            yield return response;
        }
    }

    private async Task<object> GetConfiguration()
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

    private async IAsyncEnumerable<JournalResponse> ToJournalResponseAsync<T>(IAsyncEnumerable<T> asyncEnumerable, int chunkSize)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var jsonWriter = new JsonTextWriter(writer);
        var serializer = new JsonSerializer();
        serializer.Serialize(jsonWriter, await asyncEnumerable.ToArrayAsync().ConfigureAwait(false));
        jsonWriter.Flush();
        if (memoryStream.Length < chunkSize)
        {
            yield return new JournalResponse
            {
                Chunk = memoryStream.ToArray().ToList()
            };
        }
        else
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[chunkSize];
            int readAmount;
            while ((readAmount = await memoryStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                yield return new JournalResponse
                {
                    Chunk = buffer.Take(readAmount).ToList()
                };
                buffer = new byte[chunkSize];
            }
        }
    }

    private IAsyncEnumerable<T> GetEntitiesAsync<T>(IMiddlewareRepository<T> repository, JournalRequest request)
    {
        if (request.To < 0)
        {
            return repository.GetEntriesOnOrAfterTimeStampAsync(request.From, take: (int) -request.To);
        }
        else if (request.To == 0)
        {
            return repository.GetEntriesOnOrAfterTimeStampAsync(request.From);
        }
        else
        {
            return repository.GetByTimeStampRangeAsync(request.From, request.To);
        }
    }
}