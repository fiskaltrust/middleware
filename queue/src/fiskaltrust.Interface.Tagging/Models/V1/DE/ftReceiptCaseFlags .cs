using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase))]
    public enum ftReceiptCaseFlags : long
    {
        Failed0x0001 = 0x0000_0000_0001_0000,
        Training0x0002 = 0x0000_0000_0002_0000,
        Void0x0004 = 0x0000_0000_0004_0000,
        HandWritten0x0008 = 0x0000_0000_0008_0000,
        SmallBusiness0x0010 = 0x0000_0000_0010_0000,
        ReceiverIsCompany0x00020 = 0x0000_0000_0020_0000,
        ContainsCharacteristicsRelatedToUStG0x0040 = 0x0000_0000_0040_0000,
        RequestTSEInfo0x0080 = 0x0000_0000_0080_0000,
        SelfTestClientReOrDeRegistrationly0x0100 = 0x0000_0000_0100_0000,
        RequestTSETarFileDownload0x0200 = 0x0000_0000_0200_0000,
        BypaddTSETarFileDownload0x0400 = 0x0000_0000_0400_0000,
        UploadMasterData0x0800 = 0x0000_0000_0800_0000,
        FailOnOpenTransactions0x1000 = 0x0000_0000_1000_0000,
        CloseOpenTransactions0x2000 = 0x0000_0000_2000_0000,
        BypassTSEInfoCallOnSCUSwitch0x4000 = 0x0000_0000_4000_0000,
        ImplicitTransaction0x0001_0000 = 0x0000_0001_0000_0000,
        ReceiptRequested0x8000_0000 = 0x0000_8000_0000_0000,
    }
}