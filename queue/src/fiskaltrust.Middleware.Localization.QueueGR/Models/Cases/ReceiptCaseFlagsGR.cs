using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum ReceiptCaseFlags : long
{
    HasTransportInformation = 0x0000_0000_0400_0000
}

public static class ReceiptCaseFlagsExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlags flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlags flag) => ((long) self & (long) flag) == (long) flag;
}