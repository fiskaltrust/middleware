using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.AT
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x0FFF, Prefix = "V2", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        SignatureAccordingToRKSV0x0001 = 0x001,
        DailyOperationNotification0x002= 0x002,
        FinanzOnlineNotification0x0003 = 0x003,
    }
}