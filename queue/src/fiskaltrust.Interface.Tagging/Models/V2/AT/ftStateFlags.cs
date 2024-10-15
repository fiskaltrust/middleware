using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.AT
{
    [FlagExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Prefix = "V2", CaseName = "ftStateFlag")]
    public enum ftStateFlags : long
    {
        ScuPermamentOutofService = 0x0000_0001_0000_0000,
        ScuBackup = 0x0000_0002_0000_0000,
        MonthlyClosing = 0x0000_0010_0000_0000,
        YearlyClosing = 0x0000_0020_0000_0000,
    }
}