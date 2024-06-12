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
}