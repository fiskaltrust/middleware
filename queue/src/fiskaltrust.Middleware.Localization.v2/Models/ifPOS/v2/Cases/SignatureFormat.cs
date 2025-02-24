namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum SignatureFormat : long
{
    Unknown = 0x0000,
    Text = 0x0001,
    Link = 0x0002,
    QRCode = 0x0003,
    Code128 = 0x0004,
    OcrA = 0x0005,
    Pdf417 = 0x0006,
    DataMatrix = 0x0007,
    Aztec = 0x0008,
    Ean8Barcode = 0x0009,

    Ean13 = 0x000A,
    UPCA = 0x000B,
    Code39 = 0x000C,
    Base64 = 0x000D
}

public static class SignatureFormatExt
{
    public static SignatureFormat AsSignatureFormat(this long self) => (SignatureFormat) self;
    public static T As<T>(this SignatureFormat self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);
    public static SignatureFormat Reset(this SignatureFormat self) => (SignatureFormat) (0xFFFF_F000_0000_0000 & (ulong) self);

    public static SignatureFormat WithVersion(this SignatureFormat self, byte version) => (SignatureFormat) ((((ulong) self) & 0xFFFF_0FFF_FFFF_FFFF) | ((ulong) version << (4 * 11)));
    public static byte Version(this SignatureFormat self) => (byte) ((((long) self) >> (4 * 11)) & 0xF);

    public static bool IsFormat(this SignatureFormat self, SignatureFormat format) => ((long) self & 0xFFFF) == (long) format;
    public static SignatureFormat WithFormat(this SignatureFormat self, SignatureFormat state) => (SignatureFormat) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | (ulong) state);
    public static SignatureFormat Format(this SignatureFormat self) => (SignatureFormat) ((long) self & 0xFFFF);
}
