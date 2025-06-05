using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;


public static class StateExt
{
    public static State AsState(this long self) => (State) self;
    public static T As<T>(this State self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);
    public static State Reset(this State self) => (State) (0xFFFF_F000_0000_0000 & (ulong) self);

    public static State WithVersion(this State self, byte version) => (State) ((((ulong) self) & 0xFFFF_0FFF_FFFF_FFFF) | ((ulong) version << (4 * 11)));
    public static byte Version(this State self) => (byte) ((((long) self) >> (4 * 11)) & 0xF);

    public static State WithCountry(this State self, string country)
        => country?.Length != 2
            ? throw new Exception($"'{country}' is not ISO country code")
            : self.WithCountry((country[0] << (4 * 2)) + country[1]);

    public static State WithCountry(this State self, long country) => (State) (((long) self & 0x0000_FFFF_FFFF_FFFF) | (country << (4 * 12)));
    public static long CountryCode(this State self) => (long) self >> (4 * 12);
    public static string Country(this State self)
    {
        var countryCode = self.CountryCode();

        return Char.ConvertFromUtf32((char) (countryCode & 0xFF00) >> (4 * 2)) + Char.ConvertFromUtf32((char) (countryCode & 0x00FF));
    }
    public static bool IsState(this State self, State state) => ((long) self & 0xFFFF_FFFF) == (long) state;
    public static State WithState(this State self, State state) => (State) (((ulong) self & 0xFFFF_FFFF_0000_0000) | (ulong) state);
    public static State State(this State self) => (State) ((long) self & 0xFFFF_FFFF);
}
