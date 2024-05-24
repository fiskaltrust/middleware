using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [FlagExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState))]
    public enum ftStates : long
    {

    }
}