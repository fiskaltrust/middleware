using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Prefix = "V1", CaseName = "ReceiptCaseFlag")]
    public enum ftReceiptCaseFlags : long
    {
        Failed0x0001 = 0x0000_0000_0001_0000,
        Training0x0002 = 0x0000_0000_0002_0000,
        Void0x0004 = 0x0000_0000_0004_0000,        
        ReceiptRequested0x8000_0000 = 0x0000_8000_0000_0000,
        HandWritten0x0008 = 0x0000_0000_0008_0000,
        SmallBusiness0x0010 = 0x0000_0000_0010_0000,
        ReceiverIsCompany0x00020 = 0x0000_0000_0020_0000,
        ContainsCharacteristicsRelatedToUStG0x0040 = 0x0000_0000_0040_0000,
        RequestTSEInfo0x0080 = 0x0000_0000_0080_0000,
    }
}