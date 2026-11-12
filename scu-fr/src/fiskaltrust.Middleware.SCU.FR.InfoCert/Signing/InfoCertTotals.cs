using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// Breaks a receipt down into the totals the signed data set carries: charge items by VAT class
/// and pay items by payment nature.
/// </summary>
internal readonly record struct InfoCertTotals(
    decimal Totalizer,
    decimal CINormal,
    decimal CIReduced1,
    decimal CIReduced2,
    decimal CIReducedS,
    decimal CIZero,
    decimal CIUnknown,
    decimal PICash,
    decimal PINonCash,
    decimal PIInternal,
    decimal PIUnknown)
{
    public static InfoCertTotals From(ReceiptRequest request)
    {
        var chargeItems = request.cbChargeItems ?? new List<ChargeItem>();
        var payItems = request.cbPayItems ?? new List<PayItem>();

        decimal ChargeSum(FRVatBucket bucket) => chargeItems.Where(x => FRTotalsBuckets.Of(x) == bucket).Sum(x => x.Amount);
        decimal PaySum(FRPayBucket bucket) => payItems.Where(x => FRTotalsBuckets.Of(x) == bucket).Sum(x => x.Amount);

        return new InfoCertTotals(
            Totalizer: chargeItems.Sum(x => x.Amount),
            CINormal: ChargeSum(FRVatBucket.Normal),
            CIReduced1: ChargeSum(FRVatBucket.Reduced1),
            CIReduced2: ChargeSum(FRVatBucket.Reduced2),
            CIReducedS: ChargeSum(FRVatBucket.ReducedS),
            CIZero: ChargeSum(FRVatBucket.Zero),
            CIUnknown: ChargeSum(FRVatBucket.Unknown),
            PICash: PaySum(FRPayBucket.Cash),
            PINonCash: PaySum(FRPayBucket.NonCash),
            PIInternal: PaySum(FRPayBucket.Internal),
            PIUnknown: PaySum(FRPayBucket.Unknown));
    }
}
