using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;

public enum SignatureTypePL : long
{
    FiscalDocumentNumber = 0x504C_2000_0000_0101,
    DeviceSerialNumber = 0x504C_2000_0000_0102,
    UniqueDeviceNumber = 0x504C_2000_0000_0103,
    ZReportNumber = 0x504C_2000_0000_0104,
    EReceiptReference = 0x504C_2000_0000_0105,
    StoredNotFiscalized = 0x504C_2000_0000_0106,
    // 0107/0108: e-paragon (eDokument) enrichment from the register (middleware#764) — the unique
    // eDokument id (ha) the document was bound to, and the best-effort delivery state read back
    // from the eDokument buffer (eparagonbufferget: pr = printed flag, st = delivery status).
    EDocumentId = 0x504C_2000_0000_0107,
    EDocumentDeliveryState = 0x504C_2000_0000_0108,
}

public static class SignatureTypePLExt
{
    public static T As<T>(this SignatureTypePL self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypePL signatureTypePL) => ((long) self & 0xFFFF) == ((long) signatureTypePL & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypePL state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypePL Type(this SignatureType self) => (SignatureTypePL) ((long) self & 0xFFFF);
}
