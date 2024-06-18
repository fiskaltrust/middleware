using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models
{
    [CaseExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "Country")]
    public enum ReceiptCaseCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }

    [CaseExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "Country")]
    public enum PayItemCaseCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }

    [CaseExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "Country")]
    public enum ChargeItemCaseCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }
    [CaseExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "Country")]
    public enum JournalRequestCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }

    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "Country")]
    public enum ReceiptResponseCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }

    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "TypeCountry")]
    public enum SignaturItemTypeCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }

    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Mask = 0x7FFF_0000_0000_0000, Shift = 12, CaseName = "FormatCountry")]
    public enum SignaturItemFormatCountryCodes : long
    {
        AT = 0x4154,
        DE = 0x4445,
        FR = 0x4652,
        IT = 0x4954,
        ME = 0x4D45,
    }
}