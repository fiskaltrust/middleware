using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "Version")]
    public enum ReceiptCaseVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }

    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "Version")]
    public enum ChargeItemCaseVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }

    [CaseExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "Version")]
    public enum PayItemCaseVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }

    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "FormatVersion")]
    public enum SignaturItemFormatVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "TypeVersion")]
    public enum SignaturItemTypeVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }

    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "Version")]
    public enum FtStateVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }

    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0xF000_0000_0000, Shift = 11, CaseName = "Version")]
    public enum FtJournalTypeVersions : long
    {
        V1 = 0x0,
        V2 = 0x2,
    }
}