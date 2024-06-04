using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0xFFFF)]
    public enum ftSignatureTypes : long
    {
        Signature0x0001 = 0x0001,
        ArchivingRequired0x0002 = 0x0002,
        Notification0x0003 = 0x0003,
        StartTransactionResult0x0010 = 0x0010,
        FinishTransactionPayload0x0011 = 0x0011,
        FinishTransactionResult0x0012 = 0x0012,
        ReceiptQrVersion0x0013 = 0x0013,
        ReceiptPOSSerialNumber0x0014 = 0x0014,
        ReceiptProcessType0x0015 = 0x0015,
        ReceiptProcessData0x0016 = 0x0016,
        ReceiptTransactionNumber0x0017 = 0x0017,
        ReceiptSignatureCounter0x0018 = 0x0018,
        ReceiptStartTime0x0019 = 0x0019,
        ReceiptLogTime0x001A = 0x001A,
        ReceiptSignatureAlgorithm0x001B = 0x001B,
        ReceiptLogTimeFormat0x001C = 0x001C,
        ReceiptSignature0x001D = 0x001D,
        ReceiptPublicKey0x001E = 0x001E,
        ReceiptProcessStart0x001F = 0x001F,
        UpdateTransactionPayload0x0020 = 0x0020,
        UpdateTransactionResult0x0021 = 0x0021,
        CertificationIdentification0x0022 = 0x0022,
        TSESerialNumber0x0023 = 0x0023
    }
}