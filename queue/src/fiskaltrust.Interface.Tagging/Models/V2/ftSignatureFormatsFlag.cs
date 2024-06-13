using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Prefix = "V2", CaseName = "SignatureFormatFlag")]
    public enum ftSignatureFormatsFlag : long 
    {
        AfterHeader0x0001 = 0x0000_0000_0001_0000,
        AfterChargeItemBlock0x0002 = 0x0000_0000_0002_0000,
        AfterTotalTaxBlock0x0003 = 0x0000_0000_0003_0000,
        AfterFooter0x0004 = 0x0000_0000_0004_0000,
        BeforeHeader0x0005 = 0x0000_0000_0005_0000,

    }
}