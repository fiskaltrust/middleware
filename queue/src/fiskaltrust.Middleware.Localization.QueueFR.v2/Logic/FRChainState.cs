using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>The position of one FR receipt chain: its numerator and the hash of its last entry.</summary>
public class FRChainState
{
    public FRChainState(FRReceiptChain chain)
    {
        Chain = chain;
    }

    public FRReceiptChain Chain { get; }

    public string Identifier => Chain.Identifier();

    public long Numerator { get; set; }

    public string? LastHash { get; set; }
}

/// <summary>
/// Keeps the fiscal state of the French queue: the position of every receipt chain and the
/// accumulated totals of every period. The state is reconstructed from the queue items on first
/// use - the same approach QueuePT takes - so no v1 storage column has to be shared between the
/// two French localizations.
/// </summary>
public class FRChainStateProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository;
    private readonly Dictionary<FRReceiptChain, FRChainState> _chains = new();
    private readonly FRPeriodTotalsAccumulator _totals = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _loaded;

    public FRChainStateProvider(AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository)
    {
        _queueItemRepository = queueItemRepository;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> against the chain the request belongs to and the period
    /// totals, loading both from storage on the first call. The state is held for the whole
    /// operation: an NF525 chain is strictly sequential, so numbering, signing and advancing the
    /// hash must not interleave with another receipt of the same chain.
    /// </summary>
    public async Task<T> ExecuteInChainAsync<T>(ReceiptRequest request, Func<FRChainState, FRPeriodTotalsAccumulator, Task<T>> operation)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_loaded)
            {
                await LoadAsync().ConfigureAwait(false);
                _loaded = true;
            }

            var chain = request.ResolveChain();
            if (!_chains.TryGetValue(chain, out var state))
            {
                state = new FRChainState(chain);
                _chains[chain] = state;
            }

            return await operation(state, _totals).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadAsync()
    {
        foreach (var chain in Enum.GetValues<FRReceiptChain>())
        {
            _chains[chain] = new FRChainState(chain);
        }

        // Every period starts open and is closed as soon as its closing receipt is reached, so
        // walking backwards accumulates exactly the receipts since each period's last closing.
        var openPeriods = new HashSet<FRTotalsPeriod>(Enum.GetValues<FRTotalsPeriod>());

        var queueItems = (await (await _queueItemRepository).GetAsync().ConfigureAwait(false))
            .OrderByDescending(x => x.ftQueueRow)
            .ToList();

        foreach (var queueItem in queueItems)
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }

            ReceiptRequest? request;
            ReceiptResponse? response;
            try
            {
                request = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                response = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
            }
            catch (JsonException)
            {
                continue;
            }

            if (request is null || response is null || !response.ftState.IsState(State.Success))
            {
                continue;
            }

            var state = _chains[request.ResolveChain()];
            if (state.Numerator == 0)
            {
                // Queue items are walked newest first, so the first hit per chain is its last entry.
                var numerator = ReceiptIdentificationHelper.ReadNumerator(response.ftReceiptIdentification, state.Identifier);
                if (numerator is not null)
                {
                    state.Numerator = numerator.Value;
                    state.LastHash = response.ftSignatures?.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash))?.Data;
                }
            }

            _totals.AddToOpenPeriods(request, openPeriods);

            var closedPeriod = FRPeriodTotalsAccumulator.PeriodOf(request);
            if (closedPeriod is not null)
            {
                openPeriods.Remove(closedPeriod.Value);
            }
        }
    }
}
