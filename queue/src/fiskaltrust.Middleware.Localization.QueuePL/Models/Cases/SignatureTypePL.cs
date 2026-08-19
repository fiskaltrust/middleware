using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePL.Models;

public enum SignatureTypePL : long
{
    InitialOperationReceipt = 0x504C_2000_0000_0003,
    OutOfOperationReceipt = 0x504C_2000_0000_0003,
    /// <summary>Invoice cases (0x1xxx) are persisted but not fiscalized until an SCU.PL.KSeF is configured — value shared with SCU.PL.Abstraction.</summary>
    StoredNotFiscalized = 0x504C_2000_0000_0106,
}

public static class SignatureTypePLExt
{
    public static T As<T>(this SignatureTypePL self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypePL signatureTypePL) => ((long) self & 0xFFFF) == ((long) signatureTypePL & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypePL state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypePL Type(this SignatureType self) => (SignatureTypePL) ((long) self & 0xFFFF);
}
