using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase))]
    public enum ftChargeItemCaseFlags : long
    {
        Void = 0x0000_0000_0001_0000,
        Return = 0x0000_0000_0002_0000,
        DiscountOrExtraCharge = 0x0000_0000_0004_0000,
        DownPayment = 0x0000_0000_0008_0000,
        Returnable = 0x0000_0000_0010_0000,
        TakeAway = 0x0000_0000_0020_0000,
        ShowInPayments = 0x0000_0000_8000_0000,
    }
}
