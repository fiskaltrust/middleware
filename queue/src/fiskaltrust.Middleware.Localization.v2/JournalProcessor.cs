using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.Middleware.Storage.PT;
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
    private readonly IReadOnlyConfigurationRepository _configurationRepository;
    private readonly IMiddlewareRepository<ftQueueItem> _queueItemRepository;
    private readonly IMiddlewareRepository<ftReceiptJournal> _receiptJournalRepository;
    private readonly IMiddlewareRepository<ftActionJournal> _actionJournalRepository;
    private readonly IJournalProcessor _marketSpecificJournalProcessor;
    private readonly ILogger<JournalProcessor> _logger;
    private readonly Dictionary<string, object> _configuration;

    public JournalProcessor(
        IStorageProvider storageProvider,
        IJournalProcessor marketSpecificJournalProcessor,
        Dictionary<string, object> configuration,
        ILogger<JournalProcessor> logger)
    {
        _configurationRepository = storageProvider.GetConfigurationRepository();
        _queueItemRepository = storageProvider.GetMiddlewareQueueItemRepository();
        _receiptJournalRepository = storageProvider.GetMiddlewareReceiptJournalRepository();
        _actionJournalRepository = storageProvider.GetMiddlewareActionJournalRepository();
        _marketSpecificJournalProcessor = marketSpecificJournalProcessor;
        _configuration = configuration;
        _logger = logger;
    }

    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        try
        {
            if ((0xFFFF000000000000 & (ulong) request.ftJournalType) != 0)
            {
                return _marketSpecificJournalProcessor.ProcessAsync(request);
            }

            return request.ftJournalType switch
            {
                (long) JournalTypes.ActionJournal => ToJournalResponseAsync(GetEntitiesAsync(_actionJournalRepository, request), request.MaxChunkSize),
                (long) JournalTypes.ReceiptJournal => ToJournalResponseAsync(GetEntitiesAsync(_receiptJournalRepository, request), request.MaxChunkSize),
                (long) JournalTypes.QueueItem => ToJournalResponseAsync(GetEntitiesAsync(_queueItemRepository, request), request.MaxChunkSize),
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing the Journal request.");
            throw;
        }
    }

    private async Task<object> GetConfiguration()
    {
        return new
        {
            Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
            typeof(JournalProcessor).Assembly.GetName().Version,
            CashBoxList = await _configurationRepository.GetCashBoxListAsync().ConfigureAwait(false),
            QueueList = await _configurationRepository.GetQueueListAsync().ConfigureAwait(false),
            QueueATList = await _configurationRepository.GetQueueATListAsync().ConfigureAwait(false),
            QueueDEList = await _configurationRepository.GetQueueDEListAsync().ConfigureAwait(false),
            QueueESList = string.IsNullOrEmpty(_configuration["init_ftQueueES"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftQueueES>>(_configuration["init_ftQueueES"]!.ToString()!),
            QueueFRList = await _configurationRepository.GetQueueFRListAsync().ConfigureAwait(false),
            QueueGRList = string.IsNullOrEmpty(_configuration["init_ftQueueGR"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftQueueGR>>(_configuration["init_ftQueueGR"]!.ToString()!),
            QueueITList = await _configurationRepository.GetQueueITListAsync().ConfigureAwait(false),
            QueueMEList = await _configurationRepository.GetQueueMEListAsync().ConfigureAwait(false),
            QueuePTList = string.IsNullOrEmpty(_configuration["init_ftQueuePT"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftQueuePT>>(_configuration["init_ftQueuePT"]!.ToString()!),
            SignaturCreationUnitATList = await _configurationRepository.GetSignaturCreationUnitATListAsync().ConfigureAwait(false),
            SignaturCreationUnitDEList = await _configurationRepository.GetSignaturCreationUnitDEListAsync().ConfigureAwait(false),
            SignaturCreationUnitESList = string.IsNullOrEmpty(_configuration["init_ftSignaturCreationUnitES"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftSignaturCreationUnitES>>(_configuration["init_ftSignaturCreationUnitES"]!.ToString()!),
            SignaturCreationUnitFRList = await _configurationRepository.GetSignaturCreationUnitFRListAsync().ConfigureAwait(false),
            SignaturCreationUnitGRList = string.IsNullOrEmpty(_configuration["init_ftSignaturCreationUnitGR"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftSignaturCreationUnitGR>>(_configuration["init_ftSignaturCreationUnitGR"]!.ToString()!),
            SignaturCreationUnitITList = await _configurationRepository.GetSignaturCreationUnitITListAsync().ConfigureAwait(false),
            SignaturCreationUnitMEList = await _configurationRepository.GetSignaturCreationUnitMEListAsync().ConfigureAwait(false),
            SignaturCreationUnitPTList = string.IsNullOrEmpty(_configuration["init_ftSignaturCreationUnitPT"]?.ToString()) ? [] : JsonConvert.DeserializeObject<List<ftSignaturCreationUnitPT>>(_configuration["init_ftSignaturCreationUnitPT"]!.ToString()!),
        };
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