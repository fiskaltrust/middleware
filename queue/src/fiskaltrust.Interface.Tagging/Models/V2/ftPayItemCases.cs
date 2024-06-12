using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase), Mask = 0xFFFF, Prefix = "V2", CaseName = "PayItemCase")]
    public enum ftPayItemCases : long
    {
        UnknownPaymentType0x0000 = 0x0000,
        Cash0x0001 = 0x0001,
        NonCash0x0002 = 0x0002,
        CrossedCheque0x0003 = 0x0003,
        DebitCard0x0004 = 0x0004,
        CreditCard0x0005 = 0x0005,
        Voucher0x0006 = 0x0006,
        Online0x0007 = 0x0007,
        CustomerCard0x0008 = 0x0008,
        AccountsReceivable0x0009 = 0x0009,
        SEPATransfer0x000A = 0x000A,
        OtherBankTransfer0x000B = 0x000B,
        TransferTo0x000C = 0x000C,
        InternalConsumption0x000D = 0x000D,
        Grant0x000E = 0x000E,
        Ticket0x000F = 0x000F,
    }
}