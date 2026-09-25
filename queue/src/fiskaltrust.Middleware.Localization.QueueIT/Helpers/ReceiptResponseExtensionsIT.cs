using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueueIT.Helpers;

public static class ReceiptResponseExtensionsIT
{
    public static SignatureItem? GetSignatureItem(this ReceiptResponse receiptResponse, SignatureTypeIT signatureTypeIT) => receiptResponse.ftSignatures?.FirstOrDefault(x => x.ftSignatureType.IsType(signatureTypeIT));

    public static bool HasFailed(this ReceiptResponse receiptResponse) => receiptResponse.ftState.IsState(State.Error);
}
