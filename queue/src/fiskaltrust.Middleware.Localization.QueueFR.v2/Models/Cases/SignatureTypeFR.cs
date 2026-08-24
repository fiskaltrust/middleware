using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// FR-localized signature types the queue itself emits. The values are shared with
/// <c>fiskaltrust.Middleware.SCU.FR.Abstraction</c>: the SCU produces the signature and chain-hash
/// items, the queue adds the lifecycle and information items around them.
/// </summary>
public enum SignatureTypeFR : long
{
    Information = 0x4652_2000_0000_0000,
    ReceiptSignature = 0x4652_2000_0000_0001,
    DayTotals = 0x4652_2000_0000_0003,
    MonthTotals = 0x4652_2000_0000_0004,
    YearTotals = 0x4652_2000_0000_0005,
    PerpetualTotals = 0x4652_2000_0000_0007,
    ChainHash = 0x4652_2000_0000_0008,
    StoredNotSigned = 0x4652_2000_0000_000B,
    InitialOperationReceipt = 0x4652_2000_0000_0010,
    OutOfOperationReceipt = 0x4652_2000_0000_0011,
}

public static class SignatureTypeFRExt
{
    public static T As<T>(this SignatureTypeFR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    public static bool IsType(this SignatureType self, SignatureTypeFR signatureTypeFR) => ((long) self & 0xFFFF) == ((long) signatureTypeFR & 0xFFFF);
    public static SignatureType WithType(this SignatureType self, SignatureTypeFR state) => (SignatureType) ((ulong) self & 0xFFFF_FFFF_FFFF_0000 | (ulong) state & 0xFFFF);
    public static SignatureTypeFR Type(this SignatureType self) => (SignatureTypeFR) ((long) self & 0xFFFF);
}
