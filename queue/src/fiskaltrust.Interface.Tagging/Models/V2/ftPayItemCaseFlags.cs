using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase))]
    public enum ftPayItemCaseFlags : long
    {
        Void = 0x0000_0000_0001_0000,
        Return = 0x0000_0000_0002_0000,
        Reserved = 0x0000_0000_0004_0000,
        Downpayment = 0x0000_0000_0008_0000,
        ForeignCurrency = 0x0000_0000_0010_0000,
        Change = 0x0000_0000_0020_0000,
        Tip = 0x0000_0000_0040_0000,
        Digital = 0x0000_0000_0080_0000,
        InterfaceAmountVerified = 0x0000_0000_0100_0000,
        ShowInChargeItems = 0x0000_0000_8000_0000,
    }
}