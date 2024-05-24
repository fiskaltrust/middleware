using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase))]
    public enum ftChargeItemCaseFlags : long
    {
        Lel = 0x0000_0000_0004_0000,
    }
}
