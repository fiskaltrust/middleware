using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;

/// <summary>
/// Italian (country specific) flags in the <c>ftReceiptCase</c>, shared with the SCU implementations.
/// </summary>
public enum ReceiptCaseFlagsIT : ulong
{
    /// <summary>
    /// On a <see cref="ReceiptCase.ProtocolUnspecified0x3000"/> receipt: the RT device prints the
    /// charge items as a non-fiscal document. Without the flag the protocol receipt is only stored.
    /// </summary>
    NonFiscalPrint = 0x0000_0002_0000_0000,
}

public static class ReceiptCaseFlagsITExt
{
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlagsIT flag) => ((ulong) self & (ulong) flag) == (ulong) flag;

    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlagsIT flag) => (ReceiptCase) ((ulong) self | (ulong) flag);
}
