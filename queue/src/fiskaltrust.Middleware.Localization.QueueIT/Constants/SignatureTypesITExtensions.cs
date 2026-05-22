using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.Constants;

public static class SignatureTypesITExtensions
{
    public static SignatureItem? GetSignaturItem(this ReceiptResponse receiptResponse, SignatureTypesIT signatureTypesIT) =>
        receiptResponse.ftSignatures?.FirstOrDefault(x => ((long) x.ftSignatureType & 0xFF) == (long) signatureTypesIT);
}
