using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.Storage;

public class QueueStorageProvider : IQueueStorageProvider
{
    private readonly Guid _queueId;
    private readonly IStorageProvider _storageProvider;
    private readonly CryptoHelper _cryptoHelper;
    private readonly string _processingVersion;
    private ftQueue? _cachedQueue;

    public QueueStorageProvider(Guid queueId, IStorageProvider storageProvider)
    {
        _queueId = queueId;
        _storageProvider = storageProvider;
        _cryptoHelper = new CryptoHelper();
    }

    public async Task ActivateQueueAsync()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queue.StartMoment = DateTime.UtcNow;
        await (await _storageProvider.CreateConfigurationRepository()).InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
    }

    public async Task DeactivateQueueAsync()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        queue.StopMoment = DateTime.UtcNow;
        await (await _storageProvider.CreateConfigurationRepository()).InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
    }

    public async Task<ftQueueItem> ReserveNextQueueItem(ReceiptRequest receiptRequest)
    {
        _cachedQueue ??= await GetQueueAsync();
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
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
            country = _cachedQueue.CountryCode,
            // TOdo we need to set this to the correct procsesing version
            // ProcessingVersion = _middlewareConfiguration.ProcessingVersion,
            version = "v2",
            request = JsonSerializer.Serialize(receiptRequest, jsonSerializerOptions),
        };
        if (queueItem.ftQueueTimeout == 0)
        {
            queueItem.ftQueueTimeout = 15000;
        }
        queueItem.requestHash = _cryptoHelper.GenerateBase64Hash(queueItem.request);
        await (await _storageProvider.CreateMiddlewareQueueItemRepository()).InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
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
        _cachedQueue ??= await (await _storageProvider.CreateConfigurationRepository()).GetQueueAsync(_queueId);
        return _cachedQueue;
    }

    public async Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        queueItem.response = JsonSerializer.Serialize(receiptResponse, jsonSerializerOptions);
        queueItem.responseHash = _cryptoHelper.GenerateBase64Hash(queueItem.response);
        queueItem.ftDoneMoment = DateTime.UtcNow;
        queue.ftCurrentRow++;
        await (await _storageProvider.CreateMiddlewareQueueItemRepository()).InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
        await (await _storageProvider.CreateConfigurationRepository()).InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
    }

    public async Task<long> IncrementQueueRow()
    {
        _cachedQueue ??= await GetQueueAsync();
        var queue = _cachedQueue;
        ++queue.ftQueuedRow;
        await (await _storageProvider.CreateConfigurationRepository()).InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
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
        await (await _storageProvider.CreateMiddlewareReceiptJournalRepository()).InsertAsync(receiptjournal).ConfigureAwait(false);
        queue.ftReceiptHash = receiptjournal.ftReceiptHash;
        queue.ftReceiptTotalizer += receiptjournal.ftReceiptTotal;
        await (await _storageProvider.CreateConfigurationRepository()).InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        _cachedQueue = queue;
        return receiptjournal;
    }

    public async Task CreateActionJournalAsync(string message, string type, Guid? queueItemId)
    {
        _cachedQueue ??= await (await _storageProvider.CreateConfigurationRepository()).GetQueueAsync(_queueId);
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
        await (await _storageProvider.CreateMiddlewareActionJournalRepository()).InsertAsync(actionJournal).ConfigureAwait(false);
    }

    public async Task CreateActionJournalAsync(ftActionJournal actionJournal)
    {
        _cachedQueue ??= await (await _storageProvider.CreateConfigurationRepository()).GetQueueAsync(_queueId);
        await (await _storageProvider.CreateMiddlewareActionJournalRepository()).InsertAsync(actionJournal).ConfigureAwait(false);
    }

    public async Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data)
    {
        var queueItems = (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetByReceiptReferenceAsync(data.cbReceiptReference, data.cbTerminalID).ToListAsync().ConfigureAwait(false)).OrderByDescending(x => x.TimeStamp);
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

    public async Task<(List<Receipt>?, string? error)> GetReferencedReceiptsAsync(ReceiptRequest data)
    {
        if (data.cbPreviousReceiptReference is null)
        {
            return (null, null);
        }
        var queueItemRepository = await _storageProvider.CreateMiddlewareQueueItemRepository();

        var previousReceiptReferences = data.cbPreviousReceiptReference.Match(
            single => [single],
            group => group
        );

        var previousReceiptReferenceReceipts = new List<Receipt>();

        foreach (var previousReceiptReference in previousReceiptReferences)
        {
            var previousReceiptReferenceQueueItems = await queueItemRepository.GetByReceiptReferenceAsync(previousReceiptReference).ToListAsync();
            var receipts = previousReceiptReferenceQueueItems
                .Where(qi => qi.IsReceiptRequestFinished())
                .Select(qi => new Receipt
                {
                    Request = JsonSerializer.Deserialize<ReceiptRequest>(qi.request)!,
                    Response = JsonSerializer.Deserialize<ReceiptResponse>(qi.response)!,
                }).Where(x => !x.Response.ftState.IsState(State.Error)).ToList();

            if (receipts.Count == 0)
            {
                return (null, $"The given cbPreviousReceiptReference '{previousReceiptReference}' didn't match with any of the items in the Queue or the items referenced are invalid.");
            }
            if (receipts.Count > 1)
            {
                return (null, $"The given cbPreviousReceiptReference '{previousReceiptReference}' did match with more than one item in the Queue.");
            }

            var receipt = receipts.First();
            receipt.Response.ftStateData = null;
            previousReceiptReferenceReceipts.Add(receipt);
        }

        return (previousReceiptReferenceReceipts, null);
    }
}
