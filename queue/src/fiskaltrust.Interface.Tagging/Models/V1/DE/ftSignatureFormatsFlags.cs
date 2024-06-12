using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Prefix = "V1")]
    public enum ftSignatureFormatsFlags : long
    {
        PrintingOptional0x0001 = 0x0000_0000_0001_0000,
    }
}