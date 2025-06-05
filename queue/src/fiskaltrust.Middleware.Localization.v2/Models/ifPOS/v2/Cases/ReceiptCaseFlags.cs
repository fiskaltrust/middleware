using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ReceiptCaseFlags : long
{
    LateSigning = 0x0000_0000_0001_0000,
    Training = 0x0000_0000_0002_0000,
    Void = 0x0000_0000_0004_0000,
    HandWritten = 0x0000_0000_0008_0000,
    IssuerIsSmallBusiness = 0x0000_0000_0010_0000,
    ReceiverIsBusiness = 0x0000_0000_0020_0000,
    ReceiverIsKnown = 0x0000_0000_0040_0000,
    SaleInForeignCountry = 0x0000_0000_0080_0000,
    Refund = 0x0000_0000_0100_0000,
    ReceiptRequested = 0x0000_0000_8000_0000,

    AdditionalInformationRequested = 0x0000_0000_0200_0000,
    SCUDataDownloadRequested = 0x0000_0000_0400_0000,
    EnforceServiceOperations = 0x0000_0000_0800_0000,
    CleanupOpenTransactions = 0x0000_0000_1000_0000,

    PreventEnablingOrDisablingSigningDevices = 0x0000_0000_2000_0000,
}

public static class ReceiptCaseFlagsExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlags flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlags flag) => ((long) self & (long) flag) == (long) flag;
}