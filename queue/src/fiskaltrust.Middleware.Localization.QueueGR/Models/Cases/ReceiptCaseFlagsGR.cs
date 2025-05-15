using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum ReceiptCaseFlagsGR : long
{
    IsSelfPricingOperation = 0x0100_0000_0000,
}

public static class ReceiptCaseFlagsGRExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => ((long) self & (long) flag) == (long) flag;
}