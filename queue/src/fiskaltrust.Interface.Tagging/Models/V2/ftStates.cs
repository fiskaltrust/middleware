using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState))]
    public enum ftStates : long
    {

    }
}