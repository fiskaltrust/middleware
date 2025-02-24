namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ReceiptCase : long
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

public static class ReceiptCaseExt
{
    public static ReceiptCase AsReceiptCase(this long self) => (ReceiptCase) self;
    public static T As<T>(this ReceiptCase self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);
    public static ReceiptCase Reset(this ReceiptCase self) => (ReceiptCase) (0xFFFF_F000_0000_0000 & (ulong) self);

    public static ReceiptCase WithVersion(this ReceiptCase self, byte version) => (ReceiptCase) ((((ulong) self) & 0xFFFF_0FFF_FFFF_FFFF) | ((ulong) version << (4 * 11)));
    public static byte Version(this ReceiptCase self) => (byte) ((((long) self) >> (4 * 11)) & 0xF);

    public static ReceiptCase WithCountry(this ReceiptCase self, string country)
        => country?.Length != 2
            ? throw new Exception($"'{country}' is not ISO country code")
            : self.WithCountry((country[0] << (4 * 2)) + country[1]);

    public static ReceiptCase WithCountry(this ReceiptCase self, long country) => (ReceiptCase) (((long) self & 0x0000_FFFF_FFFF_FFFF) | (country << (4 * 12)));
    public static long CountryCode(this ReceiptCase self) => (long) self >> (4 * 12);
    public static string Country(this ReceiptCase self)
    {
        var countryCode = self.CountryCode();

        return Char.ConvertFromUtf32((char) (countryCode & 0xFF00) >> (4 * 2)) + Char.ConvertFromUtf32((char) (countryCode & 0x00FF));
    }

    public static bool IsCase(this ReceiptCase self, ReceiptCase @case) => ((long) self & 0xFFFF) == (long) @case;
    public static ReceiptCase WithCase(this ReceiptCase self, ReceiptCase state) => (ReceiptCase) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | (ulong) state);
    public static ReceiptCase Case(this ReceiptCase self) => (ReceiptCase) ((long) self & 0xFFFF);
}
