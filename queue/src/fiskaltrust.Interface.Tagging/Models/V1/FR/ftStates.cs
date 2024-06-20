using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0xFFFF, Prefix = "V1", CaseName = "State")]
    public enum ftStates : long
    {
        Ready0x0000 = 0x0000,
        SecurityMechanismOutOfOperation0x0001 = 0x0001,
        MessagePending0x0040 = 0x0040,
        LateSigningModeIsActive0x0008 = 0x0008,
               
    }
}