using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
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
            _queueFR ??= await LoadAsync(repository).ConfigureAwait(false);

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

    /// <summary>
    /// The switch an 0x4011 receipt started and no 0x4012 receipt has finished yet; null when no
    /// switch is pending. Persisted in ftQueueFR; only read while the state lock is held.
    /// </summary>
    public FRScuSwitch? PendingScuSwitch
        => string.IsNullOrEmpty(_queueFR?.PendingScuSwitch) ? null : JsonSerializer.Deserialize<FRScuSwitch>(_queueFR.PendingScuSwitch);

    /// <summary>The switch that is pending, loading the state from storage on the first call.</summary>
    public async Task<FRScuSwitch?> GetPendingScuSwitchAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            _queueFR ??= await LoadAsync(await _configurationRepository).ConfigureAwait(false);
            return PendingScuSwitch;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Records that a signed receipt started (0x4011) or finished (0x4012) an SCU switch. Called
    /// with the state lock held, once the receipt is signed; stored with the chain it advanced.
    /// </summary>
    internal void TrackScuSwitch(ReceiptRequest request, ReceiptResponse response)
    {
        if (_queueFR is null)
        {
            return;
        }

        if (request.ftReceiptCase.Case() == ReceiptCase.InitSCUSwitch0x4011)
        {
            _queueFR.PendingScuSwitch = JsonSerializer.Serialize(MiddlewareStateData.FromReceiptResponse(response).FR?.ScuSwitch ?? new FRScuSwitch());
        }
        else if (request.ftReceiptCase.Case() == ReceiptCase.FinishSCUSwitch0x4012)
        {
            _queueFR.PendingScuSwitch = null;
        }
    }

    public static bool IsScuSwitch(ReceiptRequest request)
        => request.ftReceiptCase.Case() is ReceiptCase.InitSCUSwitch0x4011 or ReceiptCase.FinishSCUSwitch0x4012;

    private async Task<ftQueueFR> LoadAsync(IConfigurationRepository repository)
        => await repository.GetQueueFRAsync(_queueId).ConfigureAwait(false) ?? new ftQueueFR { ftQueueFRId = _queueId };
}
