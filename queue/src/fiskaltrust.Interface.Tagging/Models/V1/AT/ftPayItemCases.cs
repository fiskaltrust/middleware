using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [CaseExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase), Mask = 0xFFFF)]
    public enum ftPayItemCases : long
    {
        UnknownPaymentType0x0000 = 0x0000,
        Cash0x0001 = 0x0001,
        CashForeignCurrency0x0002 = 0x0002,
        CrossedCheque0x0003 = 0x0003,
        DebitCard0x0004 = 0x0004,
        CreditCard0x0005 = 0x0005,
        MultipurposeVoucher0x0006 = 0x0006,
        OnlinePayment0x0007 = 0x0007,
        CustomerCardPayment0x0008 = 0x0008,
        OtherDebitCard0x0009 = 0x0009,
        OtherCreditCard0x000A = 0x000A,
        AccountsReceivable0x000B = 0x000B,
        SEPATransfer0x000C = 0x000C,
        OtherBankTransfer0x000D = 0x000D,
        CashBookExpense0x000E = 0x000E,
        CashBookContribution0x000F = 0x000F,
        DownPayment0x0010 = 0x0010,
        InternalMaterialConsumption0x0011 = 0x0011,
        TipToEmployee0x0012 = 0x0012,
    }
}