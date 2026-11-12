using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Tells the POS which closings are due (contract §1.4 <i>State flags</i>), as <see cref="StateFlagsFR.ArchiveIsDue"/>
/// tells it an archive is due (ADR 0018). Each flag is ORed into the ftState of every signed response, training
/// included, and never blocks or signs anything:
/// <list type="bullet">
/// <item><see cref="StateFlagsFR.DailyClosingIsDue"/> 0x0100: the open day began more than 24 hours before the receipt,
/// a plain duration;</item>
/// <item><see cref="StateFlagsFR.MonthlyClosingIsDue"/> 0x0200: the open month began in an earlier calendar month;</item>
/// <item><see cref="StateFlagsFR.YearlyClosingIsDue"/> 0x0400: the open year began in an earlier calendar year.</item>
/// </list>
/// Everything is measured on <c>ftReceiptMoment</c>, in UTC, the basis of <see cref="FROpenArchiveFile.IsDue"/> and of
/// the closing periods (<see cref="FRPeriodTotalsAccumulator"/>). A period is open from the first non-training signed
/// entry after the receipt that closed it (<see cref="FRPeriodTotalsAccumulator.PeriodsClosedBy"/>: a monthly closing
/// also ends the day, a yearly closing the day and the month, 0x4001 (Queue-start receipt) and 0x3011 (Archive receipt)
/// the day), or from the queue's first entry. So a monthly closing sent late on the last day of a month, before the
/// next sale, leaves no open month behind it.
/// </summary>
public sealed class FRClosingDue
{
    private readonly ftQueueFR _queueFR;

    public FRClosingDue(ftQueueFR queueFR)
    {
        _queueFR = queueFR;
    }

    /// <summary>Folds the signed receipt into the open periods and sets the flags of the closings now due.</summary>
    public void Apply(ReceiptRequest request, ReceiptResponse response)
    {
        var moment = response.ftReceiptMoment;
        var periods = FRStateV2.Read(_queueFR).OpenPeriods ?? new OpenPeriods();

        if (!request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            var closed = FRPeriodTotalsAccumulator.PeriodsClosedBy(request);
            periods.DayStart = closed.Contains(FRTotalsPeriod.Day) ? null : periods.DayStart ?? moment;
            periods.MonthStart = closed.Contains(FRTotalsPeriod.Month) ? null : periods.MonthStart ?? moment;
            periods.YearStart = closed.Contains(FRTotalsPeriod.Year) ? null : periods.YearStart ?? moment;
            FRStateV2.Update(_queueFR, x => x.OpenPeriods = periods);
        }

        response.ftState = response.ftState.WithFlag(Due(periods, moment));
    }

    /// <summary>The flags of the closings due at <paramref name="moment"/>; 0 when none is.</summary>
    public static StateFlags Due(OpenPeriods periods, DateTime moment)
    {
        var utc = Utc(moment);
        StateFlags flags = 0;
        if (periods.DayStart is { } day && utc - Utc(day) > TimeSpan.FromHours(24))
        {
            flags |= StateFlagsFR.DailyClosingIsDue;
        }

        if (periods.MonthStart is { } month && MonthIndex(Utc(month)) < MonthIndex(utc))
        {
            flags |= StateFlagsFR.MonthlyClosingIsDue;
        }

        if (periods.YearStart is { } year && Utc(year).Year < utc.Year)
        {
            flags |= StateFlagsFR.YearlyClosingIsDue;
        }

        return flags;
    }

    /// <summary>Receipt moments are UTC; one that lost its kind in storage is taken as UTC, not as the host's local time.</summary>
    private static DateTime Utc(DateTime moment) => moment.Kind == DateTimeKind.Local ? moment.ToUniversalTime() : moment;

    private static int MonthIndex(DateTime utc) => utc.Year * 12 + utc.Month;

    /// <summary>
    /// The first entry of the open day, month and year, kept in ftQueueFR <c>StateV2</c>; null while no entry has been
    /// signed since the closing that ended the period.
    /// </summary>
    public sealed class OpenPeriods
    {
        public DateTime? DayStart { get; set; }

        public DateTime? MonthStart { get; set; }

        public DateTime? YearStart { get; set; }
    }
}
