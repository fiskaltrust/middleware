using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueBE.Models.Cases;

public enum SignatureTypeBE : long
{
    InitialOperationReceipt = 0x4245_2000_0001_1001,
    OutOfOperationReceipt = 0x4245_2000_0001_1002,
    PosReceipt = 0x4245_2000_0000_0001,
    BEInfo = 0x4245_2000_0000_0010,
}

public static class SignatureTypeBEExt
{
    public static T As<T>(this SignatureTypeBE self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeBE signatureTypeBE) => ((long) self & 0xFFFF) == ((long) signatureTypeBE & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeBE state) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | ((ulong) state & 0xFFFF));
    public static SignatureTypeBE Type(this SignatureType self) => (SignatureTypeBE) ((long) self & 0xFFFF);
}