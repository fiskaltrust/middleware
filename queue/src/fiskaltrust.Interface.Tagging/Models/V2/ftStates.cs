using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0xFFFF, Prefix = "V2", CaseName = "ftState")]
    public enum ftStates : long
    {
        SecurityMechanismOutOfOperation0x0001 = 0x0001,
        ScuTemporaryOutOfService0x0002 = 0x0002,
        LateSigningModeIsActive0x0008 = 0x0008,
        MessagePending0x0040 = 0x0040,
        DailyClosingDue0x0100 = 0x0100,
        Error0xEEEE = 0xEEEE,
        Fail0xFFFF = 0xFFFF,
    }
}