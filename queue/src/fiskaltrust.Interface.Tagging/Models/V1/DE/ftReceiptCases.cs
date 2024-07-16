using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0xFFFF, Prefix = "V1", CaseName = "ReceiptCase")]
    public enum ftReceiptCases : long
    {
        UnknownReceipt0x0000 = 0x0000,
        PointOfSaleReceipt0x0001 = 0x0001,
        PaymentTransfer0x0011 = 0x0011,

        Protocol0x000F = 0x000F,

        InvoiceB2C0x000D = 0x000D,
        InvoiceB2B0x000C = 0x000C,

        ZeroReceipt0x0002 = 0x0002,
        DailyClosing0x0007 = 0x0007,
        MonthlyClosing0x0005 = 0x0005,
        YearlyClosing0x0006 = 0x0006,

        ProtocolUnspecified0x0014 = 0x0014,

        InternalUsageMaterialConsumption0x0012 = 0x0012,
        Order0x0010 = 0x0010,

        InitialOperationReceipt0x0003 = 0x0003,
        OutOfOperationReceipt0x0004 = 0x0004,

        InitSCUSwitch0x0017 = 0x0017,
        FinishSCUSwitch0x0018 = 0x0018,

        SaleInForeignCountries0x0015 = 0x0015,

        //not mapped
        InvoiceInfo0x000E = 0x000E,
        InfoInternal0x0013 = 0x0013,
        VoidReceipt0x0016 = 0x0016,

    }
}