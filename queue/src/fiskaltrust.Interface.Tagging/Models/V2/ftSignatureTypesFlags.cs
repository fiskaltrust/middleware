using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Prefix = "V2", CaseName = "SignatureTypeFlag")]
    public enum ftSignatureTypesFlags : long
    {
        ArchivingRequired = 0x0000_0000_0001_0000,
        PrintingOptional = 0x0000_0000_0010_0000,
        DoNotPrint = 0x0000_0000_0020_0000,
        PrintedReceiptOnly = 0x0000_0000_0040_0000,
        DigitalReceiptOnly = 0x0000_0000_0080_0000,
    }
}