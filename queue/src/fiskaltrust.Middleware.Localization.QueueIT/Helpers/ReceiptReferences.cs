using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueIT.Helpers;

/// <summary>
/// Refunds, voids and reprints have to name the RT document they refer to. The v2 SignProcessor resolves
/// <c>cbPreviousReceiptReference</c> into <c>ftStateData.ftPreviousReceiptReference</c> before the market
/// processors run; this takes the RT identification of that receipt and adds it as reference signatures,
/// which is how the Italian SCUs expect to receive it.
/// </summary>
public static class ReceiptReferences
{
    /// <summary>
    /// Adds the <c>RTReference*</c> signatures of the referenced receipt to <paramref name="receiptResponse"/>.
    /// </summary>
    /// <returns>
    /// <c>false</c> when the request names a receipt the queue could not resolve (or names none although one is
    /// <paramref name="required"/>); the response is then already marked as failed.
    /// </returns>
    public static bool TryAddReferenceSignatures(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, bool required = false)
    {
        if (receiptRequest.cbPreviousReceiptReference is null)
        {
            if (required)
            {
                receiptResponse.SetReceiptResponseError(ErrorMessagesIT.PreviousReceiptReferenceRequired);
                return false;
            }
            return true;
        }

        var referencedReceipt = receiptResponse.GetPreviousReceiptReference()?.FirstOrDefault(x => !x.Response.ftState.IsState(State.Error));
        if (referencedReceipt is null)
        {
            var reference = receiptRequest.cbPreviousReceiptReference.Match(single => single, group => string.Join(", ", group));
            receiptResponse.SetReceiptResponseError(ErrorMessagesIT.ReferencedReceiptNotFound(reference));
            return false;
        }

        var documentNumber = referencedReceipt.Response.GetSignatureItem(SignatureTypeIT.RTDocumentNumber)?.Data;
        var zNumber = referencedReceipt.Response.GetSignatureItem(SignatureTypeIT.RTZNumber)?.Data;
        var documentMoment = referencedReceipt.Response.GetSignatureItem(SignatureTypeIT.RTDocumentMoment)?.Data;
        if (documentNumber is null || zNumber is null || documentMoment is null)
        {
            // The referenced receipt never got an RT identification (e.g. it was stored while the queue was not active).
            // It is passed on without references; the SCU decides how an unreferenced document is handled.
            return true;
        }

        receiptResponse.ftSignatures.AddRange(
        [
            new SignatureItem
            {
                Caption = "<reference-z-number>",
                Data = zNumber,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeIT.RTReferenceZNumber.As<SignatureType>()
            },
            new SignatureItem
            {
                Caption = "<reference-doc-number>",
                Data = documentNumber,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeIT.RTReferenceDocumentNumber.As<SignatureType>()
            },
            new SignatureItem
            {
                Caption = "<reference-timestamp>",
                Data = documentMoment,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeIT.RTReferenceDocumentMoment.As<SignatureType>()
            },
        ]);
        return true;
    }
}
