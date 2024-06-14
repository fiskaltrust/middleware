using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Mask = 0xFFFF, Prefix = "V1", CaseName = "SignatureFormat")]
    public enum ftSignatureFormats : long
    {
        Unknown0x0000 = 0x0000,
    }
}