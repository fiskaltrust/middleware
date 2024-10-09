using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2;

public class QueueStorageProvider
{
    private readonly Guid _queueId;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;
    private readonly IMiddlewareReceiptJournalRepository _middlewareReceiptJournalRepository;
    private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
    private readonly CryptoHelper _cryptoHelper;
    private ftQueue? _cachedQueue;

    public QueueStorageProvider(Guid queueId, IConfigurationRepository configurationRepository, IMiddlewareQueueItemRepository middlewareQueueItemRepository, IMiddlewareReceiptJournalRepository middlewareReceiptJournalRepository, IMiddlewareActionJournalRepository actionJournalRepository)
    {
        _queueId = queueId;
        _configurationRepository = configurationRepository;
        _middlewareQueueItemRepository = middlewareQueueItemRepository;
        _middlewareReceiptJournalRepository = middlewareReceiptJournalRepository;
        _actionJournalRepository = actionJournalRepository;
        _cryptoHelper = new CryptoHelper();
    }

    public async Task<ftQueueItem> ReserverNextQueueItem(ReceiptRequest receiptRequest)
    {
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);

        var queueItem = new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueId = _queueId,
            ftQueueMoment = DateTime.UtcNow,
            ftQueueTimeout = _cachedQueue.Timeout,
            cbReceiptMoment = receiptRequest.cbReceiptMoment,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftQueueRow = await GetNextQueueRow(),
            country = receiptRequest.GetCountry(),
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
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        return _cachedQueue.ftReceiptNumerator;
    }

    public async Task<long> GetCurrentRow()
    {
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        return _cachedQueue.ftCurrentRow;
    }
  
    public async Task<ftQueue> GetQueueAsync()
    {
        _cachedQueue ??= await _configurationRepository.GetQueueAsync(_queueId);
        return _cachedQueue;
    }

    public async Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        var queue = await _configurationRepository.GetQueueAsync(_queueId);
        queueItem.response = System.Text.Json.JsonSerializer.Serialize(receiptResponse);
        queueItem.responseHash = _cryptoHelper.GenerateBase64Hash(queueItem.response);
        queueItem.ftDoneMoment = DateTime.UtcNow;
        queue.ftCurrentRow++;
        await _middlewareQueueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
    }

    public async Task<long> GetNextQueueRow()
    {
        var queue = await _configurationRepository.GetQueueAsync(_queueId);
        ++queue.ftQueuedRow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
        return _cachedQueue.ftQueuedRow;
    }

    public async Task<ftReceiptJournal> InsertReceiptJournal(ftQueueItem queueItem, ReceiptRequest receiptrequest)
    {
        var queue = await _configurationRepository.GetQueueAsync(_queueId);
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
