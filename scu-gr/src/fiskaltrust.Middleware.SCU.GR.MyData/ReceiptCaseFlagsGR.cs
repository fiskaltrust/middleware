using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.Abstraction;

public enum ReceiptCaseFlagsGR : long
{
    HasTransportInformation = 0x0000_0000_0400_0000
}

public static class ReceiptCaseFlagsGRExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => ((long) self & (long) flag) == (long) flag;
}