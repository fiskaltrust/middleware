using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureFormat), Mask = 0xFFFF, Prefix = "V1", CaseName = "SignatureFormat")]
    public enum ftSignatureFormats : long
    {
        Unknown0x0000 = 0x0000,
        Text0x0001 = 0x0001,
        Link0x0002 = 0x0002,
        QrCode0x0003 = 0x0003,
        Code1280x0004 = 0x0004,
        OcrA0x0005 = 0x0005,
        Pdf4170x0006 = 0x0006,
        DataMatrix0x0007 = 0x0007,
        Aztec0x0008 = 0x0008,
        Ean8Barcode0x0009 = 0x0009,
        Ean130x000A = 0x000A,
        UPCA0x000B = 0x000B,
        Code390x000C = 0x000C,
        Base640x000D = 0x000D,
    }
}