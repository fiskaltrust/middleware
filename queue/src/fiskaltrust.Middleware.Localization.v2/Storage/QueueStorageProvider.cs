using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.Storage;

public class QueueStorageProvider : IQueueStorageProvider
{
    private readonly Guid _queueId;
    private readonly IStorageProvider _storageProvider;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;
    private readonly IMiddlewareReceiptJournalRepository _middlewareReceiptJournalRepository;
    private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
    private readonly CryptoHelper _cryptoHelper;
    private ftQueue? _cachedQueue;

    public QueueStorageProvider(Guid queueId, IStorageProvider storageProvider)
    {
        _queueId = queueId;
        _storageProvider = storageProvider;
        _configurationRepository = storageProvider.GetConfigurationRepository();
        _middlewareQueueItemRepository = storageProvider.GetMiddlewareQueueItemRepository();
        _middlewareReceiptJournalRepository = storageProvider.GetMiddlewareReceiptJournalRepository();
        _actionJournalRepository = storageProvider.GetMiddlewareActionJournalRepository();
        _cryptoHelper = new CryptoHelper();
    }

    public async Task ActivateQueueAsync()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queue.StartMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
    }

    public async Task DeactivateQueueAsync()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queue.StopMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
    }

    public async Task<ftQueueItem> ReserveNextQueueItem(ReceiptRequest receiptRequest)
    {
        _cachedQueue ??= await GetQueueAsync();

        var queueItem = new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueId = _queueId,
            ftQueueMoment = DateTime.UtcNow,
            ftQueueTimeout = _cachedQueue.Timeout,
            cbReceiptMoment = receiptRequest.cbReceiptMoment,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftQueueRow = await IncrementQueueRow(),
            country = receiptRequest.ftReceiptCase.Country(),
            version = "v2",
            request = System.Text.Json.JsonSerializer.Serialize(receiptRequest),
        };
        if (queueItem.ftQueueTimeout == 0)
        {
            queueItem.ftQueueTimeout = 15000;
        }
        queueItem.requestHash = _cryptoHelper.GenerateBase64Hash(queueItem.request);
        await _middlewareQueueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
        return queueItem;
    }

    public async Task<long> GetReceiptNumerator()
    {
        _cachedQueue ??= await GetQueueAsync();
        return _cachedQueue.ftReceiptNumerator;
    }

    public async Task<long> GetCurrentRow()
    {
        _cachedQueue ??= await GetQueueAsync();
        return _cachedQueue.ftCurrentRow;
    }

    public async Task<ftQueue> GetQueueAsync()
    {
        await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(5)), _storageProvider.Initialized);
        if (!_storageProvider.Initialized.IsCompleted)
        {
            throw new Exception("Storage provider is not initialized yet.");
        }
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        return _cachedQueue;
    }

    public async Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queueItem.response = System.Text.Json.JsonSerializer.Serialize(receiptResponse);
        queueItem.responseHash = _cryptoHelper.GenerateBase64Hash(queueItem.response);
        queueItem.ftDoneMoment = DateTime.UtcNow;
        queue.ftCurrentRow++;
        await _middlewareQueueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
    }

    public async Task<long> IncrementQueueRow()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        ++queue.ftQueuedRow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
        return _cachedQueue.ftQueuedRow;
    }

    public async Task<ftReceiptJournal> InsertReceiptJournal(ftQueueItem queueItem, ReceiptRequest receiptrequest)
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queue.ftReceiptNumerator++;
        var receiptjournal = new ftReceiptJournal
        {
            ftReceiptJournalId = Guid.NewGuid(),
            ftQueueId = queue.ftQueueId,
            ftQueueItemId = queueItem.ftQueueItemId,
            ftReceiptMoment = DateTime.UtcNow,
            ftReceiptNumber = queue.ftReceiptNumerator
        };
        if (receiptrequest.cbReceiptAmount.HasValue)
        {
            receiptjournal.ftReceiptTotal = receiptrequest.cbReceiptAmount.Value;
        }
        else
        {
            receiptjournal.ftReceiptTotal = (receiptrequest?.cbChargeItems?.Sum(ci => ci.Amount)).GetValueOrDefault();
        }
        receiptjournal.ftReceiptHash = _cryptoHelper.GenerateBase64ChainHash(queue.ftReceiptHash, receiptjournal, queueItem);
        await _middlewareReceiptJournalRepository.InsertAsync(receiptjournal).ConfigureAwait(false);
        queue.ftReceiptHash = receiptjournal.ftReceiptHash;
        queue.ftReceiptTotalizer += receiptjournal.ftReceiptTotal;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
        return receiptjournal;
    }

    public async Task CreateActionJournalAsync(string message, string type, Guid? queueItemId)
    {
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        var actionJournal = new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = _cachedQueue.ftQueueId,
            ftQueueItemId = queueItemId.GetValueOrDefault(),
            Message = message,
            Priority = 0,
            Type = type,
            Moment = DateTime.UtcNow
        };
        await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
    }

    public async Task CreateActionJournalAsync(ftActionJournal actionJournal)
    {
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
    }

    public async Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data)
    {
        var queueItems = (await _middlewareQueueItemRepository.GetByReceiptReferenceAsync(data.cbReceiptReference, data.cbTerminalID).ToListAsync().ConfigureAwait(false)).OrderByDescending(x => x.TimeStamp);
        foreach (var existingQueueItem in queueItems)
        {
            if (!existingQueueItem.IsReceiptRequestFinished())
            {
                continue;
            }
            if (existingQueueItem.IsContentOfQueueItemEqualWithGivenRequest(data))
            {
                return existingQueueItem;
            }
        }
        return null;
    }
}
