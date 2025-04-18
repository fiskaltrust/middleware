using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum SignatureTypeGR : long
{
    InitialOperationReceipt = 0x4752_2000_0001_1001,
    OutOfOperationReceipt = 0x4752_2000_0001_1002,
    PosReceipt = 0x4752_2000_0000_0001,
    MyDataInfo = 0x4752_2000_0000_0010,
    // TBD define signaturetypes => interface ??
}


public static class SignatureTypeGRExt
{
    public static T As<T>(this SignatureTypeGR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeGR signatureTypeGR) => ((long) self & 0xFFFF) == ((long) signatureTypeGR & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeGR state) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | ((ulong) state & 0xFFFF));
    public static SignatureTypeGR Type(this SignatureType self) => (SignatureTypeGR) ((long) self & 0xFFFF);
}