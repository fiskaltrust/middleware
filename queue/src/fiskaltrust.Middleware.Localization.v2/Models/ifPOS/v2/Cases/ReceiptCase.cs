namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;


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
