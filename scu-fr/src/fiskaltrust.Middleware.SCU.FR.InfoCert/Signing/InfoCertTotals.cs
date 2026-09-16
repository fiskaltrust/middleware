using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

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

        decimal ChargeSum(ChargeItemCase vatCase) => chargeItems.Where(x => x.ftChargeItemCase.Vat() == vatCase).Sum(x => x.Amount);
        decimal PaySum(PayItemCase payCase) => payItems.Where(x => x.ftPayItemCase.Case() == payCase).Sum(x => x.Amount);

        var known = new[]
        {
            ChargeItemCase.NormalVatRate,
            ChargeItemCase.DiscountedVatRate1,
            ChargeItemCase.DiscountedVatRate2,
            ChargeItemCase.SuperReducedVatRate1,
            ChargeItemCase.ZeroVatRate,
        };

        var knownPayCases = new[]
        {
            PayItemCase.CashPayment,
            PayItemCase.NonCash,
            PayItemCase.CrossedCheque,
            PayItemCase.DebitCardPayment,
            PayItemCase.CreditCardPayment,
            PayItemCase.VoucherPaymentCouponVoucherByMoneyValue,
            PayItemCase.OnlinePayment,
            PayItemCase.InternalMaterialConsumption,
            PayItemCase.TransferToCashbookVaultOwnerEmployee,
        };

        return new InfoCertTotals(
            Totalizer: chargeItems.Sum(x => x.Amount),
            CINormal: ChargeSum(ChargeItemCase.NormalVatRate),
            CIReduced1: ChargeSum(ChargeItemCase.DiscountedVatRate1),
            CIReduced2: ChargeSum(ChargeItemCase.DiscountedVatRate2),
            CIReducedS: ChargeSum(ChargeItemCase.SuperReducedVatRate1),
            CIZero: ChargeSum(ChargeItemCase.ZeroVatRate),
            CIUnknown: chargeItems.Where(x => !known.Contains(x.ftChargeItemCase.Vat())).Sum(x => x.Amount),
            PICash: PaySum(PayItemCase.CashPayment),
            PINonCash: payItems.Where(x => IsNonCash(x.ftPayItemCase.Case())).Sum(x => x.Amount),
            PIInternal: payItems.Where(x => IsInternal(x.ftPayItemCase.Case())).Sum(x => x.Amount),
            PIUnknown: payItems.Where(x => !knownPayCases.Contains(x.ftPayItemCase.Case())).Sum(x => x.Amount));
    }

    private static bool IsNonCash(PayItemCase payCase) => payCase is PayItemCase.NonCash
        or PayItemCase.CrossedCheque
        or PayItemCase.DebitCardPayment
        or PayItemCase.CreditCardPayment
        or PayItemCase.VoucherPaymentCouponVoucherByMoneyValue
        or PayItemCase.OnlinePayment;

    private static bool IsInternal(PayItemCase payCase) => payCase is PayItemCase.InternalMaterialConsumption
        or PayItemCase.TransferToCashbookVaultOwnerEmployee;
}
