using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.Abstraction;

public enum ReceiptCaseFlagsGR : long
{
    HasTransportInformation = 0x0000_0000_0400_0000,

    /// <summary>
    /// When set on a ReceiptCase.Pay0x3005 receipt, instructs the SCU to call the
    /// myDATA SendPaymentsMethod endpoint for an already-submitted invoice.
    /// Full receipt case: 0x4752_2000_0800_3005
    /// </summary>
    SendPaymentsMethod = 0x0000_0000_0800_0000
}

public static class ReceiptCaseFlagsGRExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlagsGR flag) => ((long) self & (long) flag) == (long) flag;
}