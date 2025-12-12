using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

public enum ReceiptCaseFlagsPT : long
{
    HasTransportInformation = 0x0000_0000_0400_0000
}

public static class ReceiptCaseFlagsPTExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlagsPT flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlagsPT flag) => ((long) self & (long) flag) == (long) flag;
}