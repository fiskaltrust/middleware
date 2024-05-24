using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase))]
    public enum ftChargeItemCaseFlags : long
    {

    }
}