namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public static class ChargeItemCaseExt
{
    public static ChargeItemCase AsChargeItemCase(this long self) => (ChargeItemCase) self;
    public static T As<T>(this ChargeItemCase self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);
    public static ChargeItemCase Reset(this ChargeItemCase self) => (ChargeItemCase) (0xFFFF_F000_0000_0000 & (ulong) self);

    public static ChargeItemCase WithVersion(this ChargeItemCase self, byte version) => (ChargeItemCase) ((((ulong) self) & 0xFFFF_0FFF_FFFF_FFFF) | ((ulong) version << (4 * 11)));
    public static byte Version(this ChargeItemCase self) => (byte) ((((long) self) >> (4 * 11)) & 0xF);

    public static ChargeItemCase WithCountry(this ChargeItemCase self, string country)
        => country?.Length != 2
            ? throw new Exception($"'{country}' is not ISO country code")
            : self.WithCountry((country[0] << (4 * 2)) + country[1]);


    public static ChargeItemCase WithCountry(this ChargeItemCase self, long country) => (ChargeItemCase) (((long) self & 0x0000_FFFF_FFFF_FFFF) | (country << (4 * 12)));
    public static long CountryCode(this ChargeItemCase self) => (long) self >> (4 * 12);
    public static string Country(this ChargeItemCase self)
    {
        var countryCode = self.CountryCode();

        return Char.ConvertFromUtf32((char) (countryCode & 0xFF00) >> (4 * 2)) + Char.ConvertFromUtf32((char) (countryCode & 0x00FF));
    }
    public static bool IsVat(this ChargeItemCase self, ChargeItemCase @case) => ((long) self & 0xF) == (long) @case;
    public static ChargeItemCase WithVat(this ChargeItemCase self, ChargeItemCase state) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_FFF0) | (ulong) state);
    public static ChargeItemCase Vat(this ChargeItemCase self) => (ChargeItemCase) ((long) self & 0xF);
}