using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueEU.Models.Cases;

public enum SignatureTypeEU : long
{
    InitialOperationReceipt = 0x4553_2000_0000_1001,
    OutOfOperationReceipt = 0x4553_2000_0000_1002,
}

public static class SignatureTypeEUExt
{
    public static T As<T>(this SignatureTypeEU self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeEU signatureTypeEU) => ((long) self & 0xFFFF) == ((long) signatureTypeEU & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeEU state) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | ((ulong) state & 0xFFFF));
    public static SignatureTypeEU Type(this SignatureType self) => (SignatureTypeEU) ((long) self & 0xFFFF);
}