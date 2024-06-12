using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase), Mask = 0xFFFF, Prefix = "V1", CaseName = "PayItemCase")]
    public enum ftPayItemCases : long
    {
        UnknownPaymentType0x0000 = 0x0000,
        Cash0x0001 = 0x0001,
        CashForeignCurrency0x0002 = 0x0002,
        CrossedCheque0x0003 = 0x0003,
        DebitCard0x0004 = 0x0004,
        CreditCard0x0005 = 0x0005,
        Online0x0006 = 0x0006,
        CustomerCard0x0007 = 0x0007,
        SEPATransfer0x0008 = 0x0008,
        OtherBankTransfer0x0009 = 0x0009,
        InternalConsumption0x000A = 0x000A,
        Change0x000B = 0x000B,
        ChangeForeignCurreny0x000C = 0x000C,
        Voucher0x000D = 0x000D,
        AccountsReceivable0x000E = 0x000E,
        DownPayment0x000F = 0x000F,
        TipToEmployee0x0010 = 0x0010,
        Grant0x0011 = 0x0011,
        CashTransferToEmptyTill0x0012 = 0x0012,
        CashTransferFromToOwner0x0013 = 0x0013,
        CashTransferFromToTill0x0014 = 0x0014,
        CashTransferToEmployee0x0015 = 0x0015,
        CashTransferFromToCashBook0x0016 = 0x0016,
        CashAmountDifferenceFromToTill0x0017 = 0x0017,
    }
}