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
    /// </summary>
    public void Add(ReceiptRequest request)
    {
        var chain = request.ResolveChain();
        if (chain is not (FRReceiptChain.Ticket or FRReceiptChain.Invoice))
        {
            return;
        }

        var totals = FRTotalsCalculator.From(request);
        foreach (var period in _closablePeriods)
        {
            AddTo(FRQueueFRColumns.TotalsPrefix(period), totals);
        }
        AddTo(chain == FRReceiptChain.Ticket ? FRQueueFRColumns.TicketTotalsPrefix : FRQueueFRColumns.InvoiceTotalsPrefix, totals);
    }

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
    /// The period a closing receipt reports, or null if the receipt is not a closing. A training
    /// closing reports no period: it attests no real turnover and must neither reset a real period
    /// nor, on replay, be taken for the boundary of one.
    /// </summary>
    public static FRTotalsPeriod? PeriodOf(ReceiptRequest request) => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training) ? null : request.ftReceiptCase.Case() switch
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
