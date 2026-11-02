using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

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
public class FRChainStateProvider
{
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly Guid _queueId;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ftQueueFR? _queueFR;

    public FRChainStateProvider(AsyncLazy<IConfigurationRepository> configurationRepository, Guid queueId)
    {
        _configurationRepository = configurationRepository;
        _queueId = queueId;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> against the chain the request belongs to and the period
    /// totals, and stores them once the chain has advanced. The state is held for the whole
    /// operation: an NF525 chain is strictly sequential, so numbering, signing and advancing the
    /// signature must not interleave with another receipt of the same chain.
    /// </summary>
    public async Task<T> ExecuteInChainAsync<T>(ReceiptRequest request, Func<FRChainState, FRPeriodTotalsAccumulator, Task<T>> operation)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var repository = await _configurationRepository;
            _queueFR ??= await repository.GetQueueFRAsync(_queueId).ConfigureAwait(false) ?? new ftQueueFR { ftQueueFRId = _queueId };

            var state = new FRChainState(request.ResolveChain(), _queueFR);
            var numeratorBefore = state.Numerator;
            T result;
            try
            {
                result = await operation(state, new FRPeriodTotalsAccumulator(_queueFR)).ConfigureAwait(false);
            }
            catch
            {
                // The operation may have moved the state before it failed; the stored state is
                // the one that matches the signed entries.
                _queueFR = null;
                throw;
            }

            // A chain only advances once its entry is signed; a failed signature rolls back.
            if (state.Numerator != numeratorBefore)
            {
                await repository.InsertOrUpdateQueueFRAsync(_queueFR).ConfigureAwait(false);
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }
}
