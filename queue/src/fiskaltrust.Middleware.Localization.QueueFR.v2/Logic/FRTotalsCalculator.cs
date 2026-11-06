using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Breaks a receipt down into the totals a French grand-total receipt reports: turnover by VAT
/// class and settlement by payment nature. The breakdown lives queue-side because the queue is
/// what accumulates it over a period - the SCUs only sign the result.
/// </summary>
public static class FRTotalsCalculator
{
    /// <remarks>
    /// Down payments are kept out of every total, on both sides: the charge and pay items of the
    /// down-payment invoice that records the acompte, and the negative ones of the final invoice
    /// that settles it. Counting them would raise the turnover when the acompte is taken and again
    /// when the sale is invoiced in full (NF525 V2.3 §6.18.1.3 as the Infocert auditor applied it,
    /// fiskaltrust/market-fr#135).
    /// </remarks>
    public static FRTotals From(ReceiptRequest request)
    {
        var chargeItems = (request.cbChargeItems ?? new List<ChargeItem>()).Where(x => !IsDownpayment(x)).ToList();
        var payItems = (request.cbPayItems ?? new List<PayItem>()).Where(x => !IsDownpayment(x)).ToList();

        decimal ChargeSum(FRVatBucket bucket) => chargeItems.Where(x => FRTotalsBuckets.Of(x) == bucket).Sum(x => x.Amount);
        decimal PaySum(FRPayBucket bucket) => payItems.Where(x => FRTotalsBuckets.Of(x) == bucket).Sum(x => x.Amount);

        return new FRTotals
        {
            Totalizer = chargeItems.Sum(x => x.Amount),
            CINormal = ChargeSum(FRVatBucket.Normal),
            CIReduced1 = ChargeSum(FRVatBucket.Reduced1),
            CIReduced2 = ChargeSum(FRVatBucket.Reduced2),
            CIReducedS = ChargeSum(FRVatBucket.ReducedS),
            CIZero = ChargeSum(FRVatBucket.Zero),
            CIUnknown = ChargeSum(FRVatBucket.Unknown),
            PICash = PaySum(FRPayBucket.Cash),
            PINonCash = PaySum(FRPayBucket.NonCash),
            PIInternal = PaySum(FRPayBucket.Internal),
            PIUnknown = PaySum(FRPayBucket.Unknown),
        };
    }

    public static void Add(this FRTotals target, FRTotals addend)
    {
        target.Totalizer += addend.Totalizer;
        target.CINormal += addend.CINormal;
        target.CIReduced1 += addend.CIReduced1;
        target.CIReduced2 += addend.CIReduced2;
        target.CIReducedS += addend.CIReducedS;
        target.CIZero += addend.CIZero;
        target.CIUnknown += addend.CIUnknown;
        target.PICash += addend.PICash;
        target.PINonCash += addend.PINonCash;
        target.PIInternal += addend.PIInternal;
        target.PIUnknown += addend.PIUnknown;
    }

    public static FRTotals Copy(this FRTotals source) => new()
    {
        Totalizer = source.Totalizer,
        CINormal = source.CINormal,
        CIReduced1 = source.CIReduced1,
        CIReduced2 = source.CIReduced2,
        CIReducedS = source.CIReducedS,
        CIZero = source.CIZero,
        CIUnknown = source.CIUnknown,
        PICash = source.PICash,
        PINonCash = source.PINonCash,
        PIInternal = source.PIInternal,
        PIUnknown = source.PIUnknown,
    };

    private static bool IsDownpayment(ChargeItem item) => ((long) item.ftChargeItemCase & (long) ChargeItemCaseFlags.Downpayment) != 0;

    private static bool IsDownpayment(PayItem item) => ((long) item.ftPayItemCase & (long) PayItemCaseFlags.Downpayment) != 0;
}
