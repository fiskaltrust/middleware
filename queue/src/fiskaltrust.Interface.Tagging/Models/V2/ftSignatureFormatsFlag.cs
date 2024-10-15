using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Prefix = "V2", CaseName = "SignatureFormatFlag")]
    public enum ftSignatureFormatsFlag : long 
    {
        AfterPayItemBlockBeforeFooter0x0 = 0x0_0000,
        AfterHeader0x1 = 0x1_0000,
        AfterChargeItemBlock0x2 = 0x2_0000,
        AfterTotalTaxBlock0x3 = 0x3_0000,
        AfterFooter0x4 = 0x4_0000,
        BeforeHeader0x5 = 0x5_0000,

    }
}