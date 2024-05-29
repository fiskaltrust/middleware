using System.Diagnostics.Metrics;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat))]
    public enum ftSignatureFormatsFlag : long 
    { 
        AfterHeader = 0x1_0000,
        AfterChargeItemBlock = 0x2_0000,
        AfterTotalTaxBlock = 0x3_0000,
        AfterFooter = 0x4_0000,
        BeforeHeader = 0x5_0000,
    }
}