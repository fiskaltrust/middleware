using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase))]
    public enum ftPayItemCaseFlags : long
    {
        Void0x0001  = 0x0000_0000_0001_0000,
        Return0x0002 = 0x0000_0000_0002_0000,
        Reserved0x0004 = 0x0000_0000_0004_0000,
        Downpayment0x0008 = 0x0000_0000_0008_0000,
        ForeignCurrency0x0010 = 0x0000_0000_0010_0000,
        Change0x0020 = 0x0000_0000_0020_0000,
        Tip0x0040 = 0x0000_0000_0040_0000,
        Digital0x0080 = 0x0000_0000_0080_0000,
        InterfaceAmountVerified0x0100 = 0x0000_0000_0100_0000,
        ShowInChargeItems0x8000 = 0x0000_0000_8000_0000,
    }
}