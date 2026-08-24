using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Accumulates the turnover a French closing receipt has to attest. Each period accumulates
/// independently and is reset by its own closing, so a monthly closing does not depend on the
/// daily ones having been sent; the perpetual total is never reset.
/// </summary>
public class FRPeriodTotalsAccumulator
{
    private readonly Dictionary<FRTotalsPeriod, FRTotals> _totals =
        Enum.GetValues<FRTotalsPeriod>().ToDictionary(period => period, _ => new FRTotals());

    /// <summary>
    /// Adds a receipt to every period that is still open. Only the sales chains contribute:
    /// grand totals, logs, provisional documents, duplicates and training must never move a
    /// fiscal total.
    /// </summary>
    public void Add(ReceiptRequest request)
    {
        if (!Contributes(request))
        {
            return;
        }

        var totals = FRTotalsCalculator.From(request);
        foreach (var period in _totals.Keys)
        {
            _totals[period].Add(totals);
        }
    }

    /// <summary>
    /// Same as <see cref="Add(ReceiptRequest)"/>, but only for the periods that are still open
    /// while replaying stored receipts backwards - a period whose closing has already been seen
    /// must not pick up receipts from before it.
    /// </summary>
    public void AddToOpenPeriods(ReceiptRequest request, IReadOnlySet<FRTotalsPeriod> openPeriods)
    {
        if (!Contributes(request))
        {
            return;
        }

        var totals = FRTotalsCalculator.From(request);
        foreach (var period in openPeriods)
        {
            _totals[period].Add(totals);
        }
    }

    /// <summary>The totals a closing of <paramref name="period"/> reports, plus the perpetual total.</summary>
    public FRPeriodTotals Snapshot(FRTotalsPeriod period) => new()
    {
        Period = period,
        Current = _totals[period].Copy(),
        Perpetual = _totals[FRTotalsPeriod.Perpetual].Copy(),
    };

    /// <summary>Closes a period. The perpetual total is never reset.</summary>
    public void Reset(FRTotalsPeriod period)
    {
        if (period != FRTotalsPeriod.Perpetual)
        {
            _totals[period] = new FRTotals();
        }
    }

    /// <summary>The period a closing receipt reports, or null if the receipt is not a closing.</summary>
    public static FRTotalsPeriod? PeriodOf(ReceiptRequest request) => request.ftReceiptCase.Case() switch
    {
        ReceiptCase.ShiftClosing0x2010 => FRTotalsPeriod.Shift,
        ReceiptCase.DailyClosing0x2011 => FRTotalsPeriod.Day,
        ReceiptCase.MonthlyClosing0x2012 => FRTotalsPeriod.Month,
        ReceiptCase.YearlyClosing0x2013 => FRTotalsPeriod.Year,
        _ => null,
    };

    /// <summary>
    /// Only the sales chains move the totals: tickets and invoices are the turnover, everything
    /// else is either a report about it or not a sale at all.
    /// </summary>
    private static bool Contributes(ReceiptRequest request) => request.ResolveChain() is FRReceiptChain.Ticket or FRReceiptChain.Invoice;
}
