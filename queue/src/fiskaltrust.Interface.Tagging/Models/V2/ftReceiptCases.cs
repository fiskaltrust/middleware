using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0xFFFF, Prefix = "V2")]
    public enum ftReceiptCases : long
    {
        UnknownReceipt0x0000 = 0x0000,
        PointOfSaleReceipt0x0001 = 0x0001,
        PaymentTransfer0x0002 = 0x0002,
        PointOfSaleReceiptWithoutObligation0x0003 = 0x0003,
        ECommerce0x0004 = 0x0004,
        Protocol0x0005 = 0x0005,

        InvoiceUnknown0x1000 = 0x1000,
        InvoiceB2C0x1001 = 0x1001,
        InvoiceB2B0x1002 = 0x1002,
        InvoiceB2G0x1003 = 0x1003,

        ZeroReceipt0x2000 = 0x2000,
        OneReceipt0x2001 = 0x2001,
        ShiftClosing0x2010 = 0x2010,
        DailyClosing0x2011 = 0x2011,
        MonthlyClosing0x2012 = 0x2012,
        YearlyClosing0x2013 = 0x2013,

        ProtocolUnspecified0x3000 = 0x3000,
        ProtocolTechnicalEvent0x3001 = 0x3001,
        ProtocolAccountingEvent0x3002 = 0x3002,
        InternalUsageMaterialConsumption0x3003 = 0x3003,
        Order0x3004 = 0x3004,
        CopyReceiptPrintExistingReceipt0x3010 = 0x3010,

        InitialOperationReceipt0x4001 = 0x4001,
        OutOfOperationReceipt0x4002 = 0x4002,
        InitSCUSwitch0x4011 = 0x4011,
        FinishSCUSwitch0x4012 = 0x4012,
    }
}