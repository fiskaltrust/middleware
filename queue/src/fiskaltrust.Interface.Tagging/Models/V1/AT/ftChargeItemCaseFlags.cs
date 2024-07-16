using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Prefix = "V1", CaseName = "ChargeItemCaseFlag")]
    public enum ftChargeItemCaseFlags : long
    {

    }
}