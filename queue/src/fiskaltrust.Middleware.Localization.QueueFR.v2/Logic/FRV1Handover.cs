using System.Globalization;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Hands a v1 queue's ftQueueFR over to the v2 queue in a half migration (ADR 0003). v2 keeps its
/// state in the same columns (market-fr ADR 0012), so the chain counters and the ticket and
/// invoice totals carry over as they stand. What differs is how the periods accumulate: v1 adds a
/// sale to the shift and the day only, and rolls the day into the month at the daily closing and
/// the month into the year at the monthly closing; v2 adds every sale to every period.
/// </summary>
/// <remarks>
/// <para>
/// A half migration starts only after the daily closing of the last v1 day (market-fr#192): it is
/// maintenance done outside business hours, like the SCU switch, so the v2 queue never inherits an
/// open v1 day and the v1 SCU signs the closing of every day it signed receipts of. The only open
/// open period left to fold at the cut is then the month, into the year.
/// </para>
/// <para>
/// For the same reason it does not start while the v1 queue is in late-signing ("failed") mode
/// (market-fr#231, decided 2026-11-04): the POS has not yet confirmed, with a zero receipt, that it
/// re-sent every receipt it issued while the middleware was unreachable, so the range would run
/// across the cut.
/// </para>
/// <para>
/// The migration receipt that calls this, and the signed migration event that records each v1
/// chain's last hash and counter, are part of the migration itself (market-fr#192).
/// </para>
/// </remarks>
public static class FRV1Handover
{
    /// <summary>
    /// Applies the handover to <paramref name="queueFR"/>, or refuses it while the last v1 day is
    /// open or the v1 late-signing mode is. Must run exactly once, before the first v2 entry: a second run would count the open
    /// month twice.
    /// </summary>
    /// <param name="lastV1DayEntryMoment">
    /// The queue moment (<c>ftQueueItem.ftQueueMoment</c>, UTC) of the last v1 receipt that counted
    /// into the day totals, or null if v1 issued none. A day can net to zero (a sale and its
    /// refund), so the day totals alone cannot show that it is closed.
    /// </param>
    /// <param name="lastV1Signatures">
    /// The Base64Url signature of each chain's last v1 entry that v1 storage still holds. A chain
    /// without one starts its v2 chaining fresh, and the migration event carries the link.
    /// </param>
    /// <returns>
    /// Null once the handover is applied; otherwise why it is refused, with
    /// <paramref name="queueFR"/> left unchanged.
    /// </returns>
    public static string? Apply(ftQueueFR queueFR, DateTime? lastV1DayEntryMoment, IReadOnlyDictionary<FRReceiptChain, string>? lastV1Signatures = null)
    {
        var refusal = Refusal(queueFR, lastV1DayEntryMoment);
        if (refusal is not null)
        {
            return refusal;
        }

        var month = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Month));
        var year = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Year));

        year.Add(month);
        queueFR.SetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Year), year);

        foreach (var (chain, signature) in lastV1Signatures ?? new Dictionary<FRReceiptChain, string>())
        {
            queueFR.SetLastSignature(chain, signature);
        }

        return null;
    }

    /// <summary>
    /// Why the v1 queue cannot be migrated yet, or null: no v1.2 archive receipt, an open v1 day, an
    /// open late-signing mode, or v1 messages still pending.
    /// </summary>
    public static string? Refusal(ftQueueFR queueFR, DateTime? lastV1DayEntryMoment)
        => ArchiveReceiptRefusal(queueFR) ?? OpenDayRefusal(queueFR, lastV1DayEntryMoment) ?? OpenLateSigningRefusal(queueFR) ?? PendingMessagesRefusal(queueFR);

    /// <summary>
    /// A v1.2 archive receipt precedes the migration (ADR 0003 amendment 2026-11-11): the v1 history is
    /// then the v1.2 archive, and the first v2 archive range starts on that receipt.
    /// </summary>
    private static string? ArchiveReceiptRefusal(ftQueueFR queueFR)
        => queueFR.ALastQueueItemId is null
            ? "The v1 queue has no archive receipt. A half migration to v2 starts only after a v1.2 archive receipt (0x3011), so that the v1 history is archived by v1.2 and the first v2 archive starts on that receipt. Send the archive receipt on the v1 queue, then migrate."
            : null;

    /// <summary>Messages a v1.2 queue still holds for the POS are returned by a v1 zero receipt only: v2 never hands them over.</summary>
    private static string? PendingMessagesRefusal(ftQueueFR queueFR)
        => queueFR.MessageCount > 0
            ? $"The v1 queue holds {queueFR.MessageCount} pending message(s) since {queueFR.MessageMoment?.ToString("u", CultureInfo.InvariantCulture) ?? "an unknown moment"}. A half migration to v2 starts only once they are read: send a zero receipt (0x2000) on the v1 queue, then migrate."
            : null;

    private static string? OpenDayRefusal(ftQueueFR queueFR, DateTime? lastV1DayEntryMoment)
    {
        var lastClosing = queueFR.GLastDayMoment;
        var day = queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(FRTotalsPeriod.Day));

        string? why = null;
        if (!IsZero(day))
        {
            why = $"its day totals hold {day.Totalizer.ToString("0.00", CultureInfo.InvariantCulture)} since {LastClosing(lastClosing)}";
        }
        else if (lastV1DayEntryMoment is { } entry && (lastClosing is null || entry > lastClosing))
        {
            why = $"a receipt was counted into the day at {entry.ToString("u", CultureInfo.InvariantCulture)}, after {LastClosing(lastClosing)}";
        }

        return why is null
            ? null
            : $"The v1 queue's last day is not closed: {why}. A half migration to v2 starts only after the daily closing (0x2011) of the last v1 day, so that the v2 queue never inherits an open v1 day. Send the daily closing on the v1 queue, then migrate.";
    }

    private static string? OpenLateSigningRefusal(ftQueueFR queueFR)
    {
        if (queueFR.UsedFailedCount <= 0 && queueFR.UsedFailedQueueItemId is null)
        {
            return null;
        }

        var range = queueFR.UsedFailedMomentMin is { } from && queueFR.UsedFailedMomentMax is { } to
            ? $" issued between {from.ToString("u", CultureInfo.InvariantCulture)} and {to.ToString("u", CultureInfo.InvariantCulture)}"
            : "";
        return $"The v1 queue is in late-signing mode: {queueFR.UsedFailedCount} late-signed receipt(s){range} since queue item {queueFR.UsedFailedQueueItemId}. A half migration to v2 starts only once that mode is closed, so that it never runs across the cut. Send a zero receipt (0x2000) on the v1 queue, then migrate.";
    }

    private static bool IsZero(FRTotals totals)
        => new[]
        {
            totals.Totalizer, totals.CINormal, totals.CIReduced1, totals.CIReduced2, totals.CIReducedS, totals.CIZero, totals.CIUnknown,
            totals.PICash, totals.PINonCash, totals.PIInternal, totals.PIUnknown,
        }.All(x => x == 0m);

    private static string LastClosing(DateTime? lastClosing) => lastClosing is { } moment
        ? $"the last daily closing at {moment.ToString("u", CultureInfo.InvariantCulture)}"
        : "the queue started, as it never had a daily closing";
}
