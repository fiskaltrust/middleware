using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The queue storage of the French queue: it stores the chain state a signature advanced only
/// once the signed queue item is stored, and drops it when the item is not stored or is stored as
/// failed. When the store fails, it reads back whether the item was kept before deciding, and when
/// that cannot be told, the next request rolls the state forward over the stored items (market-fr
/// ADR 0012). A lifecycle receipt opens or closes the queue only once its signed item is stored.
/// Everything else is the shared v2 storage unchanged.
/// </summary>
public sealed class FRQueueStorageProvider : IQueueStorageProvider
{
    private readonly IQueueStorageProvider _inner;
    private readonly FRChainStateProvider _chains;

    private readonly AsyncLazy<IMiddlewareQueueItemRepository>? _queueItems;
    private readonly AsyncLazy<IMiddlewareActionJournalRepository>? _actionJournals;

    public FRQueueStorageProvider(IQueueStorageProvider inner, FRChainStateProvider chains, AsyncLazy<IMiddlewareQueueItemRepository>? queueItems = null, AsyncLazy<IMiddlewareActionJournalRepository>? actionJournals = null)
    {
        _inner = inner;
        _chains = chains;
        _queueItems = queueItems;
        _actionJournals = actionJournals;
    }

    // The opening or closing a lifecycle receipt asked for, applied once its signed item is stored.
    private Func<Task>? _lifecycle;

    public async Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        var lifecycle = _lifecycle;
        _lifecycle = null;
        try
        {
            await _inner.FinishQueueItem(queueItem, receiptResponse).ConfigureAwait(false);
        }
        catch
        {
            // The shared storage writes the queue item before the queue row, so a failure can come
            // after the signed entry was kept: then the chain must keep it too.
            switch (await StoredAsync(queueItem, receiptResponse).ConfigureAwait(false))
            {
                case true:
                    await KeepAsync(queueItem, lifecycle).ConfigureAwait(false);
                    break;
                case false:
                    _chains.Discard();
                    break;
                default:
                    _chains.DiscardAndReconcile();
                    break;
            }

            throw;
        }

        if (receiptResponse.ftState.IsState(State.Error))
        {
            _chains.Discard();
            return;
        }

        await KeepAsync(queueItem, lifecycle).ConfigureAwait(false);
    }

    /// <summary>The signed item is stored: open or close the queue if it asked to, then store the chain state.</summary>
    private async Task KeepAsync(ftQueueItem queueItem, Func<Task>? lifecycle)
    {
        if (lifecycle is not null)
        {
            try
            {
                await lifecycle().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The item is stored but the queue row is not: replaying the item before the next
                // request applies both (FRChainStateProvider.ReconcileAsync).
                _chains.DiscardAndReconcile();
                await TryRecordAsync($"Queue item {queueItem.ftQueueItemId} is stored, but opening or closing the queue failed and is retried before the next request: {ex.Message}", queueItem.ftQueueItemId).ConfigureAwait(false);
                return;
            }
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

    /// <summary>Whether the signed item is in storage: true, false, or null when that cannot be read.</summary>
    private async Task<bool?> StoredAsync(ftQueueItem queueItem, ReceiptResponse receiptResponse)
    {
        if (_queueItems is null)
        {
            return false;
        }

        try
        {
            var stored = await (await _queueItems).GetAsync(queueItem.ftQueueItemId).ConfigureAwait(false);
            if (stored is null || string.IsNullOrEmpty(stored.response))
            {
                return false;
            }

            var response = JsonSerializer.Deserialize<ReceiptResponse>(stored.response);
            return response is not null && !response.ftState.IsState(State.Error) && response.ftReceiptIdentification == receiptResponse.ftReceiptIdentification;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Before a request is numbered: roll the chain state forward over stored entries it does not cover.</summary>
    private async Task ReconcileAsync()
    {
        if (_queueItems is null)
        {
            return;
        }

        var replayed = await _chains.ReconcileAsync(await _queueItems, ApplyLifecycleAsync, _actionJournals is null ? null : await _actionJournals).ConfigureAwait(false);
        if (replayed > 0)
        {
            await TryRecordAsync($"The French chain state was {replayed} stored signed entr{(replayed == 1 ? "y" : "ies")} behind and was rolled forward from the stored queue items.", null).ConfigureAwait(false);
        }
    }

    /// <summary>A replayed opening or closing receipt whose queue row was not written.</summary>
    private async Task ApplyLifecycleAsync(ReceiptCase receiptCase)
    {
        var queue = await _inner.GetQueueAsync().ConfigureAwait(false);
        if (receiptCase == ReceiptCase.InitialOperationReceipt0x4001 && queue.StartMoment is null)
        {
            await _inner.ActivateQueueAsync().ConfigureAwait(false);
        }
        else if (receiptCase == ReceiptCase.OutOfOperationReceipt0x4002 && queue.StopMoment is null)
        {
            await _inner.DeactivateQueueAsync().ConfigureAwait(false);
        }
    }

    private async Task TryRecordAsync(string message, Guid? queueItemId)
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

    /// <summary>Opens the queue once the signed opening entry is stored (<see cref="FinishQueueItem"/>).</summary>
    public Task ActivateQueueAsync()
    {
        _lifecycle = _inner.ActivateQueueAsync;
        return Task.CompletedTask;
    }

    /// <summary>Closes the queue once the signed closing entry is stored (<see cref="FinishQueueItem"/>).</summary>
    public Task DeactivateQueueAsync()
    {
        _lifecycle = _inner.DeactivateQueueAsync;
        return Task.CompletedTask;
    }

    public Task CreateActionJournalAsync(ftActionJournal actionJournal) => _inner.CreateActionJournalAsync(actionJournal);

    public Task CreateActionJournalAsync(string message, string type, Guid? queueItemId) => _inner.CreateActionJournalAsync(message, type, queueItemId);

    public Task<long> GetCurrentRow() => _inner.GetCurrentRow();

    public Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data) => _inner.GetExistingQueueItemOrNullAsync(data);

    public Task<Result<List<Receipt>?, string>> GetReferencedReceiptsAsync(ReceiptRequest data) => _inner.GetReferencedReceiptsAsync(data);

    public Task<long> IncrementQueueRow() => _inner.IncrementQueueRow();

    public Task<ftQueue> GetQueueAsync() => _inner.GetQueueAsync();

    public Task<long> GetReceiptNumerator() => _inner.GetReceiptNumerator();

    public Task<ftReceiptJournal> InsertReceiptJournal(ftQueueItem queueItem, ReceiptRequest receiptrequest) => _inner.InsertReceiptJournal(queueItem, receiptrequest);

    public async Task<ftQueueItem> ReserveNextQueueItem(ReceiptRequest receiptRequest)
    {
        await ReconcileAsync().ConfigureAwait(false);
        return await _inner.ReserveNextQueueItem(receiptRequest).ConfigureAwait(false);
    }

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
