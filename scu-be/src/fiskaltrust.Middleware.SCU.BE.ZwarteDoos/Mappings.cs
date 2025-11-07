using System;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public static class Mappings
{
    public static PaymentType GetPaymentType(PayItem payItem)
    {
        return payItem.ftPayItemCase.Case() switch
        {
            PayItemCase.UnknownPaymentType => PaymentType.UNKNOWN,
            PayItemCase.CashPayment => PaymentType.CASH,
            PayItemCase.DebitCardPayment => PaymentType.CARD_DEBIT,
            PayItemCase.NonCash => PaymentType.OTHER,
            PayItemCase.CrossedCheque => PaymentType.CHEQUE_OTHER,
            PayItemCase.CreditCardPayment => PaymentType.CARD_CREDIT,
            PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => PaymentType.VOUCHER_OTHER,
            PayItemCase.OnlinePayment => PaymentType.ONLINE,
            PayItemCase.LoyaltyProgramCustomerCardPayment => PaymentType.LOYALTY_REWARDS,
            PayItemCase.AccountsReceivable => throw new NotSupportedException("not supported"),
            PayItemCase.SEPATransfer => PaymentType.OTHER,
            PayItemCase.OtherBankTransfer => PaymentType.OTHER,
            PayItemCase.TransferToCashbookVaultOwnerEmployee => PaymentType.OTHER,
            PayItemCase.InternalMaterialConsumption => PaymentType.OTHER,
            PayItemCase.Grant => PaymentType.OTHER,
            PayItemCase.TicketRestaurant => PaymentType.OTHER,
            _ => PaymentType.OTHER,
        };
    }

    public static PaymentLineType GetPaymentLineType(PayItem payItem)
    {
        if (payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Tip))
        {
            return PaymentLineType.TIP;
        }

        return PaymentLineType.PAYMENT;
    }

    public static VatLabel GetVatLabelForRate(ChargeItem chargeItem)
    {
        return chargeItem.VATRate switch
        {
            21m => VatLabel.A,
            12m => VatLabel.B,
            6m => VatLabel.C,
            0m => chargeItem.ftChargeItemCase.IsVat(ChargeItemCase.ZeroVatRate) ? VatLabel.D : VatLabel.X,
            _ => throw new NotSupportedException($"VAT rate {chargeItem.VATRate} is not supported."),
        };
    }
}
