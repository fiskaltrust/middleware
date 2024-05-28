using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase))]
    public enum ftChargeItemCaseFlags : long
    {
        DownPayment = 0x0000_0000_0008_0000,
        Returnable = 0x0000_0000_0010_0000,
        DiscountOrExtraCharge = 0x0000_0000_0004_0000,
        DownPaymentCharge = 0x0000_0000_0004_0000,
    }
}
