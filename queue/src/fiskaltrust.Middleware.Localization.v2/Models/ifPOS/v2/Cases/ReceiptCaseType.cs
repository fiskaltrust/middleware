using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ReceiptCaseType : long
{
    Receipt = 0x0000,
    Invoice = 0x1000,
    DailyOperations = 0x2000,
    Log = 0x3000,
    Lifecycle = 0x4000
}

public static class ReceiptCaseTypeExt
{
    public static bool IsType(this ReceiptCase self, ReceiptCaseType type) => ((long) self & 0xF000) == (long) type;
    public static ReceiptCase WithType(this ReceiptCase self, ReceiptCaseType state) => (ReceiptCase) (((ulong) self & 0xFFFF_FFFF_FFFF_0FFF) | (ulong) state);
    public static ReceiptCaseType Type(this ReceiptCase self) => (ReceiptCaseType) ((long) self & 0xF000);
}
