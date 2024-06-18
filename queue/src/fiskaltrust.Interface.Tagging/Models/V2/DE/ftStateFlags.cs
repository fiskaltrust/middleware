using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.DE
{
    [FlagExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Prefix = "V2", CaseName = "ftStateFlag")]
    public enum ftStateFlags : long
    {
        ScuInSwitchingState = 0x0000_0001_0000_0000,
    }
}