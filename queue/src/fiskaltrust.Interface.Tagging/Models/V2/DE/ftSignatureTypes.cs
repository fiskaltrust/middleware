
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.DE
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x0FFF, Prefix = "V2", CaseName = "SignatureTypeDE")]
    public enum ftSignatureTypes : long
    {
        Signature0x001 = 0x001,
        StartTransactionResult0x010 = 0x010,
        FinishTransactionPayload0x011 = 0x011,
        FinishTransactionResult0x012 = 0x012,
        ReceiptQrVersion0x013 = 0x013,
        ReceiptPOSSerialNumber0x014 = 0x014,
        ReceiptProcessType0x015 = 0x015,
        ReceiptProcessData0x016 = 0x016,
        ReceiptTransactionNumber0x017 = 0x017,
        ReceiptSignatureCounter0x018 = 0x018,
        ReceiptStartTime0x019 = 0x019,
        ReceiptLogTime0x01A = 0x01A,
        ReceiptSignatureAlgorithm0x01B = 0x01B,
        ReceiptLogTimeFormat0x01C = 0x01C,
        ReceiptSignature0x01D = 0x01D,
        ReceiptPublicKey0x01E = 0x01E,
        ReceiptProcessStart0x01F = 0x01F,
        UpdateTransactionPayload0x020 = 0x020,
        UpdateTransactionResult0x021 = 0x021,
        CertificationIdentification0x022 = 0x022,
        TSESerialNumber0x023 = 0x023
    }
}