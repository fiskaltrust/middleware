using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat))]
    public enum ftSignatureFormatsFlag : long 
    {
        AfterHeader = 0x0000_0000_0001_0000,
        AfterChargeItemBlock = 0x0000_0000_0002_0000,
        AfterTotalTaxBlock = 0x0000_0000_0003_0000,
        AfterFooter = 0x0000_0000_0004_0000,
        BeforeHeader = 0x0000_0000_0005_0000,

    }
}