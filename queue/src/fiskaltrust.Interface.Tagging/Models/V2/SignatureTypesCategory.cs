using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0xF000, Shift = 3, Prefix = "V2Category", CaseName = "SignatureType")]
    public enum SignatureTypesCategory : long
    {
        Normal0x0 = 0x0,
        Information0x1 = 0x1,
        Alert0x2 = 0x2,
        Failure0x3 = 0x3,
    }
}