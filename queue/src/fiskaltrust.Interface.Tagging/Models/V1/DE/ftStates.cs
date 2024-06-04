using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0xFFFF)]
    public enum ftStates : long
    {
        Ready0x0000 = 0x0000,
        SecurityMechanismOutOfOperation0x0002 = 0x0002,
        ScuSwitch0x0100 = 0x0100,
    }
}