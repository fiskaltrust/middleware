using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [FlagExtensions(OnType = typeof(ReceiptRequest), OnField = nameof(ReceiptRequest.ftReceiptCase), Prefix = "V1", CaseName = "ReceiptCaseFlag")]
    public enum ftReceiptCaseFlags : long
    {

    }
}