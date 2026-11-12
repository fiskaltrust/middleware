using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The position of one FR receipt chain: its numerator and the signature of its last entry, read
/// from and written to the queue's ftQueueFR (market-fr ADR 0012).
/// </summary>
public class FRChainState
{
    private readonly ftQueueFR _queueFR;

    public FRChainState(FRReceiptChain chain) : this(chain, new ftQueueFR()) { }

    public FRChainState(FRReceiptChain chain, ftQueueFR queueFR)
    {
        Chain = chain;
        _queueFR = queueFR;
    }

    public FRReceiptChain Chain { get; }

    public string Identifier => Chain.Identifier();

    public long Numerator
    {
        get => _queueFR.GetNumerator(Chain);
        set => _queueFR.SetNumerator(Chain, value);
    }

    /// <summary>
    /// The Base64Url signature of the chain's last entry, which the next entry covers (ADR 0001);
    /// null while the chain has no signed entry yet.
    /// </summary>
    public string? LastSignature
    {
        get => _queueFR.GetLastSignature(Chain);
        set => _queueFR.SetLastSignature(Chain, value);
    }
}

/// <summary>
/// Keeps the fiscal state of the French queue: the position of every receipt chain and the
/// accumulated totals of every period. The state is persisted in the queue's ftQueueFR and read
/// once, on first use (market-fr ADR 0012); it is never rebuilt from the queue items, whose
/// number only grows.
/// </summary>
/// <remarks>
/// A signature advances the state in memory only. The state is stored by <see cref="CommitAsync"/>
/// once the signed queue item itself is stored, and dropped by <see cref="Discard"/> when it is not,
/// so the chain never points at an entry the queue did not keep (<see cref="FRQueueStorageProvider"/>
/// does both after FinishQueueItem). A French sign request runs alone from numbering to that point
/// (<see cref="SerializeAsync{T}"/>), so no other receipt can chain onto a state that may still be
/// dropped.
/// </remarks>
public class FRChainStateProvider
{
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly Guid _queueId;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private ftQueueFR? _queueFR;

    // Advanced by a signature, not stored yet, and not yet known to be kept.
    private bool _pending;

    // Kept by a stored queue item, but its write to ftQueueFR failed: it must be stored before the
    // next signature, which would otherwise be lost on a restart.
    private bool _unstored;

    // ftQueueFR may lag behind the stored queue items: at start-up (a process stopped between the two
    // writes) and after a store whose outcome is unknown. Checked before the next request is numbered.
    private bool _reconcileDue = true;

    public FRChainStateProvider(AsyncLazy<IConfigurationRepository> configurationRepository, Guid queueId)
    {
        _configurationRepository = configurationRepository;
        _queueId = queueId;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> against the chain the request belongs to, the period totals
    /// and the late-signing range. The state is held for the whole operation: an NF525 chain is strictly sequential,
    /// so numbering, signing and advancing the signature must not interleave with another receipt
    /// of the same chain. A signature that advanced the chain is kept in memory until
    /// <see cref="CommitAsync"/> or <see cref="Discard"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The state of an earlier stored receipt still cannot be written to ftQueueFR; nothing is
    /// signed until it is.
    /// </exception>
    public async Task<T> ExecuteInChainAsync<T>(ReceiptRequest request, Func<FRChainState, FRPeriodTotalsAccumulator, FRLateSigningState, FRPendingMessagesState, FROpenArchiveFile, Task<T>> operation)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var repository = await _configurationRepository;
            if (_unstored)
            {
                try
                {
                    await StoreAsync(repository).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("The French chain state of the last signed receipt could not be stored in ftQueueFR; no receipt is signed until it is.", ex);
                }
            }

            _queueFR ??= await LoadAsync(repository).ConfigureAwait(false);

            var state = new FRChainState(request.ResolveChain(), _queueFR);
            var numeratorBefore = state.Numerator;
            T result;
            try
            {
                result = await operation(state, new FRPeriodTotalsAccumulator(_queueFR), new FRLateSigningState(_queueFR), new FRPendingMessagesState(_queueFR), new FROpenArchiveFile(_queueFR)).ConfigureAwait(false);
            }
            catch
            {
                // The operation may have moved the state before it failed; the stored state is
                // the one that matches the signed entries.
                _queueFR = null;
                _pending = false;
                throw;
            }

            // A chain only advances once its entry is signed; a failed signature rolls back.
            if (state.Numerator != numeratorBefore)
            {
                _pending = true;
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Reads the chain state as the next signed receipt finds it, e.g. the previous archive receipt and
    /// the period totals an archive receipt signs (market-fr#221). Nothing it reads may be changed.
    /// </summary>
    public async Task<T> ReadAsync<T>(Func<ftQueueFR, T> read)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var repository = await _configurationRepository;
            _queueFR ??= await LoadAsync(repository).ConfigureAwait(false);
            return read(_queueFR);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Changes the state a signature just advanced, together with it: the change is stored when the
    /// signed queue item is, and dropped with it otherwise (e.g. the v1 handover of a finish-migration
    /// receipt, market-fr ADR 0003).
    /// </summary>
    /// <exception cref="InvalidOperationException">No signature advanced the state.</exception>
    public async Task ApplyToSignedStateAsync(Action<ftQueueFR> change)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_pending || _queueFR is null)
            {
                throw new InvalidOperationException("Only the state a signature advanced can be changed with it.");
            }

            change(_queueFR);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Stores the state a signature advanced, once the signed queue item is stored. If the write
    /// fails the state stays in memory and is stored before the next signature.
    /// </summary>
    public async Task CommitAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_pending)
            {
                return;
            }

            _pending = false;
            _unstored = true;
            await StoreAsync(await _configurationRepository).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Drops the state a signature advanced, because its queue item was not stored or was stored
    /// as failed. The next signature continues from the stored state.
    /// </summary>
    public void Discard()
    {
        _lock.Wait();
        try
        {
            if (_pending)
            {
                _queueFR = null;
                _pending = false;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Drops the in-memory state because it is unknown whether the signed queue item was stored. The
    /// stored state is read again and rolled forward over the stored items before the next request
    /// (<see cref="ReconcileAsync"/>), so a stored entry is never numbered again.
    /// </summary>
    public void DiscardAndReconcile()
    {
        _lock.Wait();
        try
        {
            _queueFR = null;
            _pending = false;
            _reconcileDue = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Rolls ftQueueFR forward over the signed entries stored after the state it holds:
    /// walks back from the last stored queue item to the first one the state
    /// already covers, replays the newer ones in order (<see cref="FRChainRepair"/>), runs the
    /// lifecycle step of a replayed opening or closing receipt, and stores the result. Runs once at
    /// start-up and again after a store whose outcome was unknown; returns the number of entries replayed.
    /// </summary>
    /// <exception cref="InvalidOperationException">ftQueueFR is missing, or the repaired state cannot be stored.</exception>
    public async Task<int> ReconcileAsync(IMiddlewareQueueItemRepository queueItems, Func<ReceiptCase, Task> lifecycle, IMiddlewareActionJournalRepository? actionJournals = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_reconcileDue)
            {
                return 0;
            }

            var repository = await _configurationRepository;
            if (_unstored)
            {
                await StoreAsync(repository).ConfigureAwait(false);
            }

            _queueFR ??= await LoadAsync(repository).ConfigureAwait(false);

            var ahead = new List<FRChainRepair.SignedEntry>();
            var last = await queueItems.GetLastQueueItemAsync().ConfigureAwait(false);
            for (var row = last?.ftQueueRow ?? 0; row > 0; row--)
            {
                var item = row == last!.ftQueueRow ? last : await queueItems.GetByQueueRowAsync(row).ConfigureAwait(false);
                if (item is null)
                {
                    continue;
                }

                var (kind, entry) = FRChainRepair.Read(item);
                if (kind == FRChainRepair.Kind.V1)
                {
                    break;
                }

                if (entry is null)
                {
                    continue;
                }

                if (entry.Number <= _queueFR.GetNumerator(entry.Chain))
                {
                    break;
                }

                ahead.Add(entry);
            }

            ahead.Reverse();
            foreach (var entry in ahead)
            {
                foreach (var file in FRChainRepair.Apply(_queueFR, entry))
                {
                    await RecordForcedArchiveAsync(entry, file, actionJournals).ConfigureAwait(false);
                }
                if (!entry.Request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
                {
                    await lifecycle(entry.Request.ftReceiptCase.Case()).ConfigureAwait(false);
                }
            }

            if (ahead.Count > 0)
            {
                try
                {
                    await StoreAsync(repository).ConfigureAwait(false);
                }
                catch
                {
                    _queueFR = null;
                    throw;
                }
            }

            _reconcileDue = false;
            return ahead.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// The journal entries of a forced file met while rolling forward (ADR 0018). The limit and the
    /// closing rule are deterministic over the stored entries, so the file is the one the lost store
    /// recorded. An entry stored with the receipt is kept and never written twice; the pending count is
    /// replayed either way, since ftQueueFR is what lagged.
    /// </summary>
    private async Task RecordForcedArchiveAsync(FRChainRepair.SignedEntry entry, FROpenArchiveFile.ForcedFile file, IMiddlewareActionJournalRepository? actionJournals)
    {
        if (actionJournals is null)
        {
            return;
        }

        var stored = await actionJournals.GetByQueueItemId(entry.Response.ftQueueItemID).ToListAsync().ConfigureAwait(false);
        var forced = stored.FirstOrDefault(x => FRArchiveDue.IsForcedArchiveOf(x, file));
        if (forced is null)
        {
            forced = Factories.ftActionJournalFactory.CreateForcedArchiveActionJournal(entry.Request, entry.Response, file);
            await actionJournals.InsertAsync(forced).ConfigureAwait(false);
        }

        var pending = FRArchiveDue.PendingJet30(new FRPendingMessagesState(_queueFR!), entry.Response, file, forced.ftActionJournalId);
        if (!stored.Any(x => FRArchiveDue.IsPendingJet30Of(x, file)))
        {
            await actionJournals.InsertAsync(pending).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The queue's ftQueueFR row. A missing row is a configuration error (the queue's
    /// <c>init_ftQueueFR</c>), never a blank row: a blank one would be stored over the configuration
    /// by the next commit, without SIRET or SCU.
    /// </summary>
    private async Task<ftQueueFR> LoadAsync(IConfigurationRepository repository)
        => await repository.GetQueueFRAsync(_queueId).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The French queue {_queueId} has no ftQueueFR row. It is part of the queue's configuration (init_ftQueueFR); nothing is signed until it exists.");

    /// <summary>
    /// Records a JET event found outside a request (a local archive export's integrity check, a
    /// rollback) as a pending message: the action journal entry is stored first, then the count in
    /// ftQueueFR (<see cref="FRPendingMessagesState"/>). Runs between requests, so no signed but
    /// unstored chain state is written with it.
    /// </summary>
    public async Task<ftActionJournal> RaisePendingJetEventAsync(IMiddlewareActionJournalRepository actionJournals, int code, string label, string? information)
    {
        await _requestLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var repository = await _configurationRepository;
                if (_pending || _unstored)
                {
                    throw new InvalidOperationException("The French chain state of the last signed receipt is not stored yet; the pending JET event is raised once it is.");
                }

                _queueFR ??= await LoadAsync(repository).ConfigureAwait(false);
                var journal = new FRPendingMessagesState(_queueFR).Raise(_queueId, code, label, information, DateTime.UtcNow);
                try
                {
                    await actionJournals.InsertAsync(journal).ConfigureAwait(false);
                    await StoreAsync(repository).ConfigureAwait(false);
                }
                catch
                {
                    _queueFR = null;
                    throw;
                }

                return journal;
            }
            finally
            {
                _lock.Release();
            }
        }
        finally
        {
            _requestLock.Release();
        }
    }

    /// <summary>
    /// Runs one French sign request alone, from reserving its queue item to storing it, so that the
    /// state it advances is committed or discarded before another receipt is chained onto it.
    /// </summary>
    public async Task<T> SerializeAsync<T>(Func<Task<T>> request)
    {
        await _requestLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await request().ConfigureAwait(false);
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private async Task StoreAsync(IConfigurationRepository repository)
    {
        await repository.InsertOrUpdateQueueFRAsync(_queueFR!).ConfigureAwait(false);
        _unstored = false;
    }
}
