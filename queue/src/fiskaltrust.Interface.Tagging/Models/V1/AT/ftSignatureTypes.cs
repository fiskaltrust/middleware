using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0xFFFF, Prefix = "V1", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        Unknown0x0000 = 0x0000,
        SignatureAccordingToRKSV0x0001 = 0x0001,
        ArchivingRequired0x0002 = 0x0002,
        FinanzOnlineNotification0x0003 = 0x0003,
    }
}