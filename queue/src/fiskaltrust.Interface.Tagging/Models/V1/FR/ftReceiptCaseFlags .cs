using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Prefix = "V1")]
    public enum ftReceiptCaseFlags : long
    {
        Failed0x0001 = 0x0000_0000_0001_0000,
        Training0x0002 = 0x0000_0000_0002_0000,
        Void0x0004 = 0x0000_0000_0004_0000,        
        ReceiptRequested0x8000_0000 = 0x0000_8000_0000_0000,
    }
}