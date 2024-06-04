using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat))]
    public enum ftSignatureFormatsFlags : long
    {
        PrintingOptional = 0x0000_0000_0001_0000,
    }
}