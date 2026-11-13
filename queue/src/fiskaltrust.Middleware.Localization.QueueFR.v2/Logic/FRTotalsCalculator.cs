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
    /// when the sale is invoiced in full (NF525 V2.3 §6.18.1.3, fiskaltrust/market-fr#135).
    /// <para>
    /// A multipurpose voucher sold (a not-taxable, V=8, voucher, S=4, charge item) is kept out of the turnover too: it
    /// is a treasury operation, and the sale it pays for counts when the voucher is redeemed (NF525 V2.3 §6.18.1.3,
    /// market-fr#221). A single-purpose voucher is taxed when sold and stays in. The pay items stay as they are: they record the
    /// money received, so on a multipurpose-voucher sale the charge and pay totals no longer balance, by design. The payment-
    /// proof totals (1.2 <c>P</c> columns) leave the voucher out too.
    /// </para>
    /// </remarks>
    public static FRTotals From(ReceiptRequest request)
    {
        var chargeItems = (request.cbChargeItems ?? new List<ChargeItem>()).Where(x => !IsDownpayment(x) && !IsMultipurposeVoucher(x)).ToList();
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
            // market-fr#221 (ADR 0021, R13 grand totals): the same charge items per VAT rate, so 2.1 % is no longer merged into ReducedS.
            ByRate = chargeItems.GroupBy(x => FRTotals.RateKey(x.VATRate)).ToDictionary(x => x.Key, x => x.Sum(item => item.Amount)),
        };
    }

    public static void Add(this FRTotals target, FRTotals addend)
    {
        foreach (var (rate, amount) in addend.ByRate)
        {
            target.ByRate[rate] = (target.ByRate.TryGetValue(rate, out var sum) ? sum : 0m) + amount;
        }
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
        ByRate = new Dictionary<string, decimal>(source.ByRate),
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

    /// <summary>A multipurpose voucher sold: a voucher (S=4) charge item that is not taxable (V=8). A taxed voucher is single-purpose.</summary>
    public static bool IsMultipurposeVoucher(ChargeItem item)
        => item.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.Voucher && item.ftChargeItemCase.Vat() == ChargeItemCase.NotTaxable;
}
