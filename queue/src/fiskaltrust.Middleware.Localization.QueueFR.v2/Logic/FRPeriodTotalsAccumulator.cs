using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Accumulates the turnover a French closing receipt has to attest. Each period accumulates
/// independently, so a monthly closing does not depend on the daily ones having been sent; the
/// perpetual total is never reset. A closing resets its own period and, as in v1.2 (workerFR
/// <c>MonthRequest</c>, <c>YearRequest</c>, <c>StartRequest</c>, <c>StopRequest</c>), the shorter
/// periods it ends too (<see cref="PeriodsClosedBy"/>, decided 2026-11-04): a monthly closing also
/// closes the day, a yearly closing the day and the month, the initial-operation receipt the day,
/// and the out-of-operation receipt the day, the month and the year. A shift and a daily closing
/// close only themselves. The totals live in the v1
/// columns of ftQueueFR (market-fr ADR 0012), so a half-migrated queue carries them over as they are.
/// </summary>
public class FRPeriodTotalsAccumulator
{
    private static readonly FRTotalsPeriod[] _closablePeriods = [FRTotalsPeriod.Shift, FRTotalsPeriod.Day, FRTotalsPeriod.Month, FRTotalsPeriod.Year];

    private readonly ftQueueFR _queueFR;

    public FRPeriodTotalsAccumulator() : this(new ftQueueFR()) { }

    public FRPeriodTotalsAccumulator(ftQueueFR queueFR)
    {
        _queueFR = queueFR;
    }

    /// <summary>
    /// Adds a receipt to every period. Only the sales chains contribute: grand totals, logs,
    /// provisional documents, duplicates and training must never move a fiscal total.
    /// <para>
    /// As in v1.2, the other chains still keep their own 1.2 totalizers next to them, so the portal
    /// and other readers of the 1.2 columns stay current after a half migration (market-fr#221):
    /// bills and foreign sales in <c>B*</c>, payment proofs in <c>PTotalizer</c>/<c>PPI*</c>,
    /// training in <c>XTotalizer</c>, and the sales since the last archive in <c>A*</c>.
    /// </para>
    /// </summary>
    public void Add(ReceiptRequest request)
    {
        var chain = request.ResolveChain();
        switch (chain)
        {
            case FRReceiptChain.Ticket or FRReceiptChain.Invoice:
                var totals = FRTotalsCalculator.From(request);
                foreach (var period in _closablePeriods)
                {
                    AddTo(FRQueueFRColumns.TotalsPrefix(period), totals);
                }
                AddTo(chain == FRReceiptChain.Ticket ? FRQueueFRColumns.TicketTotalsPrefix : FRQueueFRColumns.InvoiceTotalsPrefix, totals);
                AddTo(ArchiveTotalsPrefix, totals);
                break;
            case FRReceiptChain.Bill:
                AddTo(BillTotalsPrefix, FRTotalsCalculator.From(request));
                break;
            case FRReceiptChain.PaymentProof:
                var proof = FRTotalsCalculator.From(request);
                _queueFR.PTotalizer += proof.Totalizer;
                _queueFR.PPITotalCash += proof.PICash;
                _queueFR.PPITotalNonCash += proof.PINonCash;
                _queueFR.PPITotalInternal += proof.PIInternal;
                _queueFR.PPITotalUnknown += proof.PIUnknown;
                break;
            case FRReceiptChain.Training:
                _queueFR.XTotalizer += FRTotalsCalculator.From(request).Totalizer;
                break;
        }
    }

    private const string BillTotalsPrefix = "B";

    private const string ArchiveTotalsPrefix = "A";

    /// <summary>The totals a closing of <paramref name="period"/> reports, plus the perpetual total.</summary>
    public FRPeriodTotals Snapshot(FRTotalsPeriod period) => new()
    {
        Period = period,
        Current = period == FRTotalsPeriod.Perpetual ? Perpetual() : _queueFR.GetTotals(FRQueueFRColumns.TotalsPrefix(period)),
        Perpetual = Perpetual(),
    };

    /// <summary>Closes a period. The perpetual total is never reset.</summary>
    public void Reset(FRTotalsPeriod period)
    {
        if (period != FRTotalsPeriod.Perpetual)
        {
            _queueFR.SetTotals(FRQueueFRColumns.TotalsPrefix(period), new FRTotals());
        }
    }

    /// <summary>
    /// Closes a period and records, in the 1.2 columns, which receipt closed it and when
    /// (v1.2 <c>GLast{Shift,Day,Month,Year}Moment</c> and <c>…QueueItemId</c>).
    /// </summary>
    public void Close(FRTotalsPeriod period, DateTime moment, Guid queueItemId)
    {
        Reset(period);
        switch (period)
        {
            case FRTotalsPeriod.Shift: _queueFR.GLastShiftMoment = moment; _queueFR.GLastShiftQueueItemId = queueItemId; break;
            case FRTotalsPeriod.Day: _queueFR.GLastDayMoment = moment; _queueFR.GLastDayQueueItemId = queueItemId; break;
            case FRTotalsPeriod.Month: _queueFR.GLastMonthMoment = moment; _queueFR.GLastMonthQueueItemId = queueItemId; break;
            case FRTotalsPeriod.Year: _queueFR.GLastYearMoment = moment; _queueFR.GLastYearQueueItemId = queueItemId; break;
        }
    }

    /// <summary>
    /// The periods a signed receipt closes, its own and the shorter ones it ends (see the class
    /// summary). Empty for a receipt that closes nothing, and for every training receipt.
    /// </summary>
    public static IReadOnlyList<FRTotalsPeriod> PeriodsClosedBy(ReceiptRequest request)
    {
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return [];
        }

        return request.ftReceiptCase.Case() switch
        {
            ReceiptCase.ShiftClosing0x2010 => [FRTotalsPeriod.Shift],
            ReceiptCase.DailyClosing0x2011 => [FRTotalsPeriod.Day],
            ReceiptCase.MonthlyClosing0x2012 => [FRTotalsPeriod.Day, FRTotalsPeriod.Month],
            ReceiptCase.YearlyClosing0x2013 => [FRTotalsPeriod.Day, FRTotalsPeriod.Month, FRTotalsPeriod.Year],
            ReceiptCase.InitialOperationReceipt0x4001 => [FRTotalsPeriod.Day],
            ReceiptCase.OutOfOperationReceipt0x4002 => [FRTotalsPeriod.Day, FRTotalsPeriod.Month, FRTotalsPeriod.Year],
            _ => [],
        };
    }

    /// <summary>
    /// The period a real closing receipt closes, or null if the receipt is not one. A training
    /// closing closes no period: it attests no real turnover and must neither reset a real period
    /// nor, on replay, be taken for the boundary of one.
    /// </summary>
    public static FRTotalsPeriod? PeriodOf(ReceiptRequest request) => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training) ? null : ReportedPeriodOf(request);

    /// <summary>
    /// The period whose totals a closing receipt prints, training or not. As in v1.2, a training
    /// closing prints the live figures of the real period without closing it (market-fr#221).
    /// </summary>
    public static FRTotalsPeriod? ReportedPeriodOf(ReceiptRequest request) => request.ftReceiptCase.Case() switch
    {
        ReceiptCase.ShiftClosing0x2010 => FRTotalsPeriod.Shift,
        ReceiptCase.DailyClosing0x2011 => FRTotalsPeriod.Day,
        ReceiptCase.MonthlyClosing0x2012 => FRTotalsPeriod.Month,
        ReceiptCase.YearlyClosing0x2013 => FRTotalsPeriod.Year,
        _ => null,
    };

    /// <summary>The ticket and invoice totals are never reset, so together they are the perpetual total.</summary>
    private FRTotals Perpetual()
    {
        var perpetual = _queueFR.GetTotals(FRQueueFRColumns.TicketTotalsPrefix);
        perpetual.Add(_queueFR.GetTotals(FRQueueFRColumns.InvoiceTotalsPrefix));
        return perpetual;
    }

    private void AddTo(string prefix, FRTotals totals)
    {
        var accumulated = _queueFR.GetTotals(prefix);
        accumulated.Add(totals);
        _queueFR.SetTotals(prefix, accumulated);
    }
}
