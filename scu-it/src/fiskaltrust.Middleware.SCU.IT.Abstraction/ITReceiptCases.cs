namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public enum ITReceiptCases
{
    UnknownReceipt0x00 = 0x000,
    POSReceipt0x001 = 0x001,
    CashDepositPayInn0x002 = 0x002,
    CashPayOut0x003 = 0x003,
    PaymentTransfer0x004 = 0x004,
    POSReceiptWithoutCashRegisterObligation0x005 = 0x005,
    ECommerce0x006 = 0x006,
    SaleInForeignCountry0x007 = 0x007,

    InitialOperationReceipt0xF01 = 0xF01,
    OutOfOperationReceipt0xF02 = 0xF02,

    ZeroReceipt0x200 = 0x200,
    ShiftClosing0x211 = 0x211,
    DailyClosing0x212 = 0x212,
    MonthlyClosing0x213 = 0x213,
    YearlyClosing0x214 = 0x214,

    InvoiceUnpsecified0x101 = 0x101,
    InvoiceB2B0x102 = 0x102,
    InvoiceB2C0x103 = 0x103,
    InvoiceB2G0x104 = 0x104,

    ProtocolTechnicalEvent0x301 = 0x301,
    ProtocolAccountingEvent0x302 = 0x302,
    ProtoclUnspecified0x303 = 0x303,
    InternalUsageMaterialConsumption0x304 = 0x304,
}

public static class SIgna