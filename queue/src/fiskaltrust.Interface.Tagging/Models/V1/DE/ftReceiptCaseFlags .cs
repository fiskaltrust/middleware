using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase))]
    public enum ftReceiptCaseFlags : long
    {
        Failed = 0x0000000000010000,
        Training = 0x0000000000020000,
        Void = 0x0000000000040000,
        HandWritten = 0x0000000000080000,
        SmallBusiness = 0x0000000000100000,
        ReceiverIsCompany = 0x0000000000200000,
        ContainsCharacteristicsRelatedToUStG = 0x0000000000400000,
        RequestTSEInfo = 0x0000000000800000,
        RequestSelfTest = 0x0000000001000000,
        ClientIdRegistrationly = 0x0000000001000000,
        ClientIdDeregistrationly = 0x0000000001000000,
        RequestTSETarFileDownload = 0x0000000002000000,
        BypaddTSETarFileDownload = 0x0000000004000000,
        UploadMasterData = 0x0000000008000000,
        FailOnOpenTransactions = 0x0000000010000000,
        CloseOpenTransactions = 0x0000000020000000,
        BypassTSEInfoCallOnSCUSwitch = 0x0000000040000000,
        ImplicitTransaction = 0x0000000100000000,
        ReceiptRequested = 0x0000800000000000,
    }
}