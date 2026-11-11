using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
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
    public async Task<T> ExecuteInChainAsync<T>(ReceiptRequest request, Func<FRChainState, FRPeriodTotalsAccumulator, FRLateSigningState, Task<T>> operation)
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

            _queueFR ??= await repository.GetQueueFRAsync(_queueId).ConfigureAwait(false) ?? new ftQueueFR { ftQueueFRId = _queueId };

            var state = new FRChainState(request.ResolveChain(), _queueFR);
            var numeratorBefore = state.Numerator;
            T result;
            try
            {
                result = await operation(state, new FRPeriodTotalsAccumulator(_queueFR), new FRLateSigningState(_queueFR)).ConfigureAwait(false);
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
            _queueFR ??= await repository.GetQueueFRAsync(_queueId).ConfigureAwait(false) ?? new ftQueueFR { ftQueueFRId = _queueId };
            return read(_queueFR);
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
