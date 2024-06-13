using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Prefix = "V2", CaseName = "ReceiptCaseFlag")]
    public enum ftReceiptCaseFlags : long
    {
        LateSigning0x0001 = 0x0000_0000_0001_0000,
        Training0x0002 = 0x0000_0000_0002_0000,
        Void0x0004 = 0x0000_0000_0004_0000,
        HandWritten0x0008 = 0x0000_0000_0008_0000,
        IssuerIsSmallBusiness0x0010 = 0x0000_0000_0010_0000,
        ReceiverIsBusiness0x0020 = 0x0000_0000_0020_0000,
        ReceiverIsKnown0x0040 = 0x0000_0000_0040_0000,
        SaleInForeignCountry0x0080 = 0x0000_0000_0080_0000,
        Return0x0100 = 0x0000_0000_0100_0000,
        ReceiptRequested0x8000 = 0x0000_0000_8000_0000,

        AdditionalInformationRequested0x0200 = 0x0000_0000_0200_0000,
        SCUDataDownloadRequested0x0400 = 0x0000_0000_0400_0000,
        EnforceServiceOperations0x0800 = 0x0000_0000_0800_0000,
        CleanupOpenTransactions0x1000 = 0x0000_0000_1000_0000,

        PreventEnablingOrDisablingSigningDevices0x2000 = 0x0000_0000_2000_0000,
    }
}