using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Stores in <c>ftStateData.FR.Closing</c> which period a receipt of the closing chains S, Z, M, Y or the technical
/// chain E closes (archive-v2 §1.3: <c>ClosingType</c>, <c>PeriodStart</c>, <c>PeriodEnd</c>; R13 period identifier).
/// </summary>
/// <remarks>
/// <para>
/// A closing closes its own period. A lifecycle receipt closes the longest period it ends
/// (<see cref="FRPeriodTotalsAccumulator.PeriodsClosedBy"/>): the day for an initial-operation receipt, the year for an
/// out-of-operation receipt. A zero receipt, an SCU switch or a migration receipt closes none and carries only its type.
/// </para>
/// <para>
/// The period starts where its totals were last reset: the previous closing of that period, including one that ended it
/// by cascade (a monthly closing also ends the day), else the queue's start, else the receipt itself. It ends on the
/// receipt's own moment. Nothing here is signed: signing the period start would change the LNE data set frozen on
/// 2026-11-10, and that decision is open.
/// </para>
/// </remarks>
public static class FRClosingPeriod
{
    /// <param name="totals">The period state before this receipt closes anything.</param>
    /// <param name="queueStart">When the queue started (<c>ftQueue.StartMoment</c>), where a never-closed period begins.</param>
    public static void Apply(ReceiptRequest request, ReceiptResponse response, FRReceiptChain chain, FRPeriodTotalsAccumulator totals, DateTime? queueStart)
    {
        if (Of(request, chain) is not { } closing)
        {
            return;
        }

        var (type, period) = closing;
        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.Closing = new FRStateClosing
        {
            ClosingType = type,
            PeriodStart = period is { } closed ? totals.LastClosed(closed) ?? queueStart ?? response.ftReceiptMoment : null,
            PeriodEnd = period is null ? null : response.ftReceiptMoment,
        };
        response.ftStateData = stateData;
    }

    private static (string Type, FRTotalsPeriod? Period)? Of(ReceiptRequest request, FRReceiptChain chain) => chain switch
    {
        FRReceiptChain.ShiftClosing => ("shift", FRTotalsPeriod.Shift),
        FRReceiptChain.DailyClosing => ("daily", FRTotalsPeriod.Day),
        FRReceiptChain.MonthlyClosing => ("monthly", FRTotalsPeriod.Month),
        FRReceiptChain.YearlyClosing => ("yearly", FRTotalsPeriod.Year),
        FRReceiptChain.Technical => (request.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle) ? "lifecycle" : "zero", LongestClosedBy(request)),
        _ => null,
    };

    private static FRTotalsPeriod? LongestClosedBy(ReceiptRequest request)
    {
        var closed = FRPeriodTotalsAccumulator.PeriodsClosedBy(request).Where(x => x != FRTotalsPeriod.Archive).ToList();
        return closed.Count == 0 ? null : closed.Max();
    }
}
