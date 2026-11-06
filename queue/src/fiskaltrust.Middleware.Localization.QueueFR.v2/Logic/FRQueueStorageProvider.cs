using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The queue storage of the French queue: it stores the chain state a signature advanced only
/// once the signed queue item is stored, and drops it when the item is not stored or is stored as
/// failed. Everything else is the shared v2 storage unchanged.
/// </summary>
public sealed class FRQueueStorageProvider : IQueueStorageProvider
{
    private readonly IQueueStorageProvider _inner;
    private readonly FRChainStateProvider _chains;

    public FRQueueStorageProvider(IQueueStorageProvider inner, FRChainStateProvider chains)
    {
        _inner = inner;
        _chains = chains;
    }

    public async Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        try
        {
            await _inner.FinishQueueItem(queueItem, receiptResponse).ConfigureAwait(false);
        }
        catch
        {
            // The signed entry is not in the queue: the chain must not point at it.
            _chains.Discard();
            throw;
        }

        if (receiptResponse.ftState.IsState(State.Error))
        {
            _chains.Discard();
            return;
        }

        try
        {
            await _chains.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The receipt is stored and valid; only the chain state is not. It stays in memory, is
            // stored before the next signature, and nothing is signed until it is.
            await TryRecordAsync($"The French chain state after queue item {queueItem.ftQueueItemId} could not be stored in ftQueueFR and is retried before the next signature: {ex.Message}", queueItem.ftQueueItemId).ConfigureAwait(false);
        }
    }

    private async Task TryRecordAsync(string message, Guid queueItemId)
    {
        try
        {
            await _inner.CreateActionJournalAsync(message, $"{0x4652_0000_0000_0000:X}-ChainStateNotStored", queueItemId).ConfigureAwait(false);
        }
        catch
        {
            // Storage is failing as a whole; the retry before the next signature still holds.
        }
    }

    public Task ActivateQueueAsync() => _inner.ActivateQueueAsync();

    public Task DeactivateQueueAsync() => _inner.DeactivateQueueAsync();

    public Task CreateActionJournalAsync(ftActionJournal actionJournal) => _inner.CreateActionJournalAsync(actionJournal);

    public Task CreateActionJournalAsync(string message, string type, Guid? queueItemId) => _inner.CreateActionJournalAsync(message, type, queueItemId);

    public Task<long> GetCurrentRow() => _inner.GetCurrentRow();

    public Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data) => _inner.GetExistingQueueItemOrNullAsync(data);

    public Task<Result<List<Receipt>?, string>> GetReferencedReceiptsAsync(ReceiptRequest data) => _inner.GetReferencedReceiptsAsync(data);

    public Task<long> IncrementQueueRow() => _inner.IncrementQueueRow();

    public Task<ftQueue> GetQueueAsync() => _inner.GetQueueAsync();

    public Task<long> GetReceiptNumerator() => _inner.GetReceiptNumerator();

    public Task<ftReceiptJournal> InsertReceiptJournal(ftQueueItem queueItem, ReceiptRequest receiptrequest) => _inner.InsertReceiptJournal(queueItem, receiptrequest);

    public Task<ftQueueItem> ReserveNextQueueItem(ReceiptRequest receiptRequest) => _inner.ReserveNextQueueItem(receiptRequest);

    public Task<List<(ReceiptRequest, ReceiptResponse)>> GetReceiptReferencesIfNecessaryAsync(ProcessCommandRequest request) => _inner.GetReceiptReferencesIfNecessaryAsync(request);
}

/// <summary>
/// Runs every French sign request alone, so the chain state one request advances is committed or
/// discarded (<see cref="FRQueueStorageProvider"/>) before the next request is numbered.
/// </summary>
public sealed class FRSignProcessor : ISignProcessor
{
    private readonly ISignProcessor _inner;
    private readonly FRChainStateProvider _chains;

    public FRSignProcessor(ISignProcessor inner, FRChainStateProvider chains)
    {
        _inner = inner;
        _chains = chains;
    }

    public Task<ReceiptResponse?> ProcessAsync(ReceiptRequest request) => _chains.SerializeAsync(() => _inner.ProcessAsync(request));
}
