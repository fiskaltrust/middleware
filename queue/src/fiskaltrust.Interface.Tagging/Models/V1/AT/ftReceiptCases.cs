using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0xFFFF, Prefix = "V1", CaseName = "ReceiptCase")]
    public enum ftReceiptCases : long
    {
        UnknownReceiptType0x0000 = 0x0000,
        POSReceipt0x0001 = 0x0001,
        CashDepositCashPayIn0x000A = 0x000A,
        CashPayOut0x000B = 0x000B,
        PaymentTransfer0x000C = 0x000C,
        POSReceiptWithoutCashRegisterObligation0x0007 = 0x0007,
        ECommerce0x000F = 0x000F,
        ProtocolArtefactHandedOutToConsumer0x0009 = 0x0009,
        InvoiceUnspecifiedType0x0008 = 0x0008,
        ZeroReceipt0x0002 = 0x0002,
        MonthlyClosing0x0005 = 0x0005,
        YearlyClosing0x0006 = 0x0006,
        ProtocolUnspecifiedType0x000D = 0x000D,
        InternalUsageMaterialConsumption0x000E = 0x000E,
        InitialOperationReceipt0x0003 = 0x0003,
        OutOfOperationReceipt0x0004 = 0x0004,
        SaleInForeignCountries0x0010 = 0x0010,
    }
}