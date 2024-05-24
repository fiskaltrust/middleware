using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase))]
    public enum ftReceiptCaseFlags : long
    {
        LateSigning = 0x0000_0000_0001_000,
        Training = 0x0000_0000_0002_0000,
        Void = 0x0000_0000_0004_0000,
        HandWritten = 0x0000_0000_0008_0000,
        IssuerIsSmallBusiness = 0x0000_0000_0010_0000,
        ReceiverIsBusiness = 0x0000_0000_0020_0000,
        ReceiverIsKnown = 0x0000_0000_0040_0000,
        SaleInForeignCountry = 0x0000_0000_0080_0000,
        Return = 0x0000_0000_0100_0000,
        ReceiptRequested = 0x0000_0000_8000_0000,

        AdditionalInformationRequested = 0x0000_0000_0200_0000,
        SCUDataDownloadRequested = 0x0000_0000_0400_0000,
        EnforceServiceOperations = 0x0000_0000_0800_0000,
        CleanupOpenTransactions = 0x0000_0000_1000_0000,

        PreventEnablingOrDisablingSigningDevices = 0x0000_0000_2000_0000,
    }
}