using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0xFFFF, Prefix = "V2", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        NormalNotification0x0000 = 0x0000,
        NormalMarketCompliance0x0001 = 0x0001,
        InformationNotification0x1000 = 0x1000,
        InformationMarketCompliance0x1001 = 0x1001,
        AlertNotification0x1000 = 0x2000,
        AlertMarketCompliance0x1001 = 0x2001,
        FailureNotification0x1000 = 0x3000,
        FailureMarketCompliance0x1001 = 0x3001,
    }
}