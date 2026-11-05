using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>The turnover bucket of a charge item in the French totals.</summary>
public enum FRVatBucket
{
    Normal,
    Reduced1,
    Reduced2,
    ReducedS,
    Zero,
    Unknown,
}

/// <summary>The settlement bucket of a pay item in the French totals.</summary>
public enum FRPayBucket
{
    Cash,
    NonCash,
    Internal,
    Unknown,
}

/// <summary>
/// The single mapping from a v2 charge or pay item to its French totals bucket, shared by the
/// queue (period totals) and the InfoCert SCU (signed receipt totals) so the two cannot drift.
/// It follows QueueFR v1.2 as reached through the PosSystem API v2→v0 translation, except where
/// an ADR says otherwise (doc/nf525/adr in fiskaltrust/market-fr: card, crossed cheque and voucher
/// stay non-cash).
/// </summary>
public static class FRTotalsBuckets
{
    public static FRVatBucket Of(ChargeItem item) => item.ftChargeItemCase.Vat() switch
    {
        ChargeItemCase.UnknownService => FromRate(item.VATRate),
        ChargeItemCase.DiscountedVatRate1 => FRVatBucket.Reduced1,
        ChargeItemCase.DiscountedVatRate2 => FRVatBucket.Reduced2,
        ChargeItemCase.NormalVatRate => FRVatBucket.Normal,
        ChargeItemCase.SuperReducedVatRate1 => FRVatBucket.ReducedS,
        // v1.2 reaches these through v0 "zero": super-reduced 2, parking and not taxable have no
        // French rate of their own.
        ChargeItemCase.SuperReducedVatRate2 => FRVatBucket.Zero,
        ChargeItemCase.ParkingVatRate => FRVatBucket.Zero,
        ChargeItemCase.ZeroVatRate => FRVatBucket.Zero,
        ChargeItemCase.NotTaxable => FRVatBucket.Zero,
        _ => FRVatBucket.Unknown,
    };

    /// <summary>v1.2 derives the class of an unspecified VAT case from its rate.</summary>
    private static FRVatBucket FromRate(decimal vatRate) => vatRate switch
    {
        5.5m => FRVatBucket.Reduced1,
        10m => FRVatBucket.Reduced2,
        20m => FRVatBucket.Normal,
        0m => FRVatBucket.Zero,
        _ => FRVatBucket.ReducedS,
    };

    public static FRPayBucket Of(PayItem item)
    {
        // v1.2 compares the whole case, so a tip never matches a bucket.
        if (item.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip))
        {
            return FRPayBucket.Unknown;
        }

        return item.ftPayItemCase.Case() switch
        {
            // v1.2: "handled like a cash payment".
            PayItemCase.UnknownPaymentType => FRPayBucket.Cash,
            PayItemCase.CashPayment => FRPayBucket.Cash,
            // v1.2 reaches v0 06 (voucher), which it counts as cash.
            PayItemCase.TicketRestaurant => FRPayBucket.Cash,
            PayItemCase.NonCash => FRPayBucket.NonCash,
            // Cash in v1.2; non-cash in v2 by decision (ADR "card, cheque and voucher are non-cash").
            PayItemCase.CrossedCheque => FRPayBucket.NonCash,
            PayItemCase.CreditCardPayment => FRPayBucket.NonCash,
            PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => FRPayBucket.NonCash,
            PayItemCase.DebitCardPayment => FRPayBucket.NonCash,
            PayItemCase.OnlinePayment => FRPayBucket.NonCash,
            PayItemCase.LoyaltyProgramCustomerCardPayment => FRPayBucket.NonCash,
            PayItemCase.SEPATransfer => FRPayBucket.NonCash,
            PayItemCase.OtherBankTransfer => FRPayBucket.NonCash,
            PayItemCase.AccountsReceivable => FRPayBucket.Internal,
            PayItemCase.TransferToCashbookVaultOwnerEmployee => FRPayBucket.Internal,
            PayItemCase.InternalMaterialConsumption => FRPayBucket.Internal,
            PayItemCase.Grant => FRPayBucket.Internal,
            _ => FRPayBucket.Unknown,
        };
    }
}
