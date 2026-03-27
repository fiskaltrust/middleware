using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.Abstraction;

public enum SignatureTypeGR : long
{
    PosReceipt = 0x4752_2000_0000_0001,
    ProviderSignature = 0x4752_2000_0000_0010,
    UniqueDocumentIdentifier = 0x4752_2000_0000_0011,
    TransmissionFailure_1 = 0x4752_2000_0000_0012,
    MultipleConnectedMarks = 0x4752_2000_0000_0013,
    OrderReceiptSignature = 0x4752_2000_0000_0014,
    Uid = 0x4752_2000_0000_0015,
    Mark = 0x4752_2000_0000_0016,
    AuthenticatioNCode = 0x4752_2000_0000_0017,
    GenericMyDataInfo = 0x4752_2000_0000_0018,
    QRCode = 0x4752_2000_0000_0019,
    MyDataXML = 0x4752_2000_0000_0050,
}

public static class SignatureTypeGRExt
{
    public static T As<T>(this SignatureTypeGR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeGR signatureTypeGR) => ((long) self & 0xFFFF) == ((long) signatureTypeGR & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeGR state) => (SignatureType) (((ulong) self & 0xFFFF_FFFF_FFFF_0000) | ((ulong) state & 0xFFFF));
    public static SignatureTypeGR Type(this SignatureType self) => (SignatureTypeGR) ((long) self & 0xFFFF);
}