using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ChargeItem), OnField = nameof(ChargeItem.ftChargeItemCase), Prefix = "V2", CaseName = "ChargeItemCaseFlag")]
    public enum ftChargeItemCaseFlags : long
    {
        Void0x0001 = 0x0000_0000_0001_0000,
        Return0x0002 = 0x0000_0000_0002_0000,
        DiscountOrExtraCharge0x0004 = 0x0000_0000_0004_0000,
        DownPayment0x0008 = 0x0000_0000_0008_0000,
        Returnable0x0010 = 0x0000_0000_0010_0000,
        TakeAway0x0020 = 0x0000_0000_0020_0000,
        ShowInPayments0x8000 = 0x0000_0000_8000_0000,
    }
}
