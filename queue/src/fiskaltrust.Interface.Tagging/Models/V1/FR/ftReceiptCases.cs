using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;
namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0xFFFF, CaseName = "V1ftReceiptCase")]
    public enum ftReceiptCases : long
    {
        UnknownReceipt0x0000 = 0x0000,
        PointOfSaleReceipt0x0001 = 0x0001,
        PaymentTransfer0x000C = 0x000C,
        Protocol0x0009 = 0x0009,

        InvoiceUnknown0x0003 = 0x0003,

        ZeroReceipt0x000F = 0x000F,
        OneReceipt0x2001 = 0x2001,
        ShiftClosing0x0004 = 0x0004,
        DailyClosing0x0005 = 0x0005,
        MonthlyClosing0x0006 = 0x0006,
        YearlyClosing0x0007 = 0x0007,

        ProtocolUnspecified0x0014 = 0x0014,
        ProtocolTechnicalEvent0x0012 = 0x0012,
        ProtocolAccountingEvent0x0013 = 0x0013,
        InternalUsageMaterialConsumption0x000D = 0x000D,

        InitialOperationReceipt0x0010 = 0x0010,
        OutOfOperationReceipt0x0011 = 0x0011,

        PaymentProve0x0002 = 0x0002,
        Bill0x0008 = 0x0008,
        Archive0x0015 = 0x0015,
        Copy0x0016 = 0x0016,

        SaleInForeignCountries0x000E = 0x000E,

    }
}