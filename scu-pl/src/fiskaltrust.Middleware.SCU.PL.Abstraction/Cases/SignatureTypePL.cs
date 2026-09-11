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
    /// <summary>A non-fiscal printout the register produced for the receipt — a goods return (zwrot towaru), which has no fiscal document number.</summary>
    NonFiscalPrintout = 0x504C_2000_0000_0107,
    // 0108/0109: e-paragon (eDokument) enrichment from the register (middleware#764) — the unique
    // eDokument id (ha) the document was bound to, and the best-effort delivery state read back
    // from the eDokument buffer (eparagonbufferget: pr = printed flag, st = delivery status).
    EDocumentId = 0x504C_2000_0000_0108,
    EDocumentDeliveryState = 0x504C_2000_0000_0109,
    /// <summary>
    /// The additional lines requested via ftReceiptCaseData.PL.printout were not (fully) printed: the
    /// register rejected a trftrln after the receipt was already closed, so the fiscal document stands
    /// and this item carries the device's error instead of failing the receipt.
    /// </summary>
    AdditionalPrintoutNotPrinted = 0x504C_2000_0000_010A,
}

public static class SignatureTypePLExt
{
    public static T As<T>(this SignatureTypePL self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypePL signatureTypePL) => ((long) self & 0xFFFF) == ((long) signatureTypePL & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypePL state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypePL Type(this SignatureType self) => (SignatureTypePL) ((long) self & 0xFFFF);
}
