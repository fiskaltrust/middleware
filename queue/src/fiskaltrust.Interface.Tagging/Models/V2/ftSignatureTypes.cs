using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x0FFF, Prefix = "V2", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        Notification0x000 = 0x0000,
        MarketCompliance0x0001 = 0x0001,
        Unknown0x0000 = 0x0000,
    }
}