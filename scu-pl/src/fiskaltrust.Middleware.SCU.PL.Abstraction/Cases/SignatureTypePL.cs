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
    /// <summary>
    /// The number the register filed the fiscal document under in its protected memory (pamięć chroniona) —
    /// the anchor that ties the receipt to the register's own archive. One sequence across all document
    /// types, so it differs from the receipt number and has to be kept alongside it.
    /// </summary>
    ProtectedMemoryDocumentNumber = 0x504C_2000_0000_0108,
    /// <summary>The register's own clock at the fiscal document, ISO 8601 with offset — never the host's clock.</summary>
    DeviceMoment = 0x504C_2000_0000_0109,
    /// <summary>
    /// The daily report a fiscal document is booked into — the one still to be printed. Kept apart
    /// from <see cref="ZReportNumber"/>, which a daily closing signs for the report it did print.
    /// </summary>
    CurrentDailyReportNumber = 0x504C_2000_0000_010A,
}

public static class SignatureTypePLExt
{
    public static T As<T>(this SignatureTypePL self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypePL signatureTypePL) => ((long) self & 0xFFFF) == ((long) signatureTypePL & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypePL state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypePL Type(this SignatureType self) => (SignatureTypePL) ((long) self & 0xFFFF);
}
