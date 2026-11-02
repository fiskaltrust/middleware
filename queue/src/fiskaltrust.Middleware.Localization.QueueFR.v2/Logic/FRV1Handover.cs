using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Hands a v1 queue's ftQueueFR over to the v2 queue in a half migration (ADR 0003). v2 keeps its
/// state in the same columns (market-fr ADR 0012), so the chain counters and the ticket and
/// invoice totals carry over as they stand. What differs is how the periods accumulate: v1 adds a
/// sale to the shift and the day only, and rolls the day into the month at the daily closing and
/// the month into the year at the monthly closing; v2 adds every sale to every period. The open
/// lower periods are therefore folded into the higher ones once, at the cut.
/// </summary>
/// <remarks>
/// The migration receipt that calls this, and the signed migration event that records each v1
/// chain's last hash and counter, are part of the migration itself (market-fr#192).
/// </remarks>
public static class FRV1Handover
{
    /// <summary>
    /// Applies the handover to <paramref name="queueFR"/>. Must run exactly once, before the first
    /// v2 entry: a second run would count the open day twice.
    /// </summary>
    /// <param name="lastV1Signatures">
    /// The Base64Url signature of each chain's last v1 entry that v1 storage still holds. A chain
    /// without one starts its v2 chaining fresh, and the migration event carries the link.
    /// </param>
    public static void Apply(ftQueueFR queueFR, IReadOnlyDictionary<FRReceiptChain, string>? lastV1Signatures = null)
    {
        var day = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Day));
        var month = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Month));
        var year = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Year));

        month.Add(day);
        year.Add(month);
        queueFR.SetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Month), month);
        queueFR.SetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Year), year);

        foreach (var (chain, signature) in lastV1Signatures ?? new Dictionary<FRReceiptChain, string>())
        {
            queueFR.SetLastSignature(chain, signature);
        }
    }
}
