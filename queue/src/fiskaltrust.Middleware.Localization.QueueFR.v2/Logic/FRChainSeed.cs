using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>The position a v1 queue hands over for one receipt chain.</summary>
/// <param name="Numerator">The chain counter of the chain's last v1 entry; the first v2 entry takes the next one.</param>
/// <param name="LastSignature">
/// The Base64Url signature of the chain's last v1 entry, when v1 storage still holds it; null
/// otherwise. In that case the signed migration event carries the link from v1 to v2 (ADR 0003).
/// </param>
public record FRChainSeedEntry(long Numerator, string? LastSignature);

/// <summary>
/// The fiscal state a v1 queue carries over in a half migration (ADR 0003): the position of every
/// chain and the totals of every period at the cut. The receipts themselves do not move, so the
/// v2 queue cannot rebuild this from its own entries; it reads the seed once, when it first loads.
/// </summary>
public class FRChainSeed
{
    public IReadOnlyDictionary<FRReceiptChain, FRChainSeedEntry> Chains { get; init; } = new Dictionary<FRReceiptChain, FRChainSeedEntry>();

    /// <summary>
    /// The totals of every period at the cut, since its last v1 closing. The perpetual total must
    /// be included: a migrated queue is the same queue, so its grand total is never restarted.
    /// </summary>
    public IReadOnlyDictionary<FRTotalsPeriod, FRTotals> Totals { get; init; } = new Dictionary<FRTotalsPeriod, FRTotals>();
}

/// <summary>
/// Where a migrated queue reads its <see cref="FRChainSeed"/> from. A queue that was opened on v2
/// has no seed source, or one that returns null.
/// </summary>
/// <remarks>
/// This is the seam only. The migration receipt that writes the seed, and the source that reads
/// it back, are part of the migration itself (market-fr#192).
/// </remarks>
public interface IFRChainSeedSource
{
    Task<FRChainSeed?> GetAsync();
}
