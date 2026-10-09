using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public static class ReceiptResponseHelper
{
    public static List<Receipt> GetRequiredPreviousReceiptReference(this ReceiptResponse receiptResponse)
    {
        var middlewareStateData = MiddlewareStateData.FromReceiptResponse(receiptResponse);
        if(middlewareStateData?.PreviousReceiptReference == null)
        {
            throw new InvalidOperationException("PreviousReceiptReference is required but was not found in the ReceiptResponse.");
        }
        return middlewareStateData.PreviousReceiptReference;
    }

    public static List<Receipt>? GetPreviousReceiptReference(this ReceiptResponse receiptResponse)
    {
        var middlewareStateData = MiddlewareStateData.FromReceiptResponse(receiptResponse);
        return middlewareStateData?.PreviousReceiptReference;
    }

    public static void SetReceiptResponseError(this ReceiptResponse receiptResponse, string errorMessage)
    {
        receiptResponse.ftState = receiptResponse.ftState.WithState(State.Error);
        receiptResponse.ftSignatures = [];
        receiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "FAILURE",
            Data = errorMessage,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = receiptResponse.ftState.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure)
        });
    }

    public static void MarkAsDisabled(this ReceiptResponse receiptResponse)
    {
        receiptResponse.ftState = receiptResponse.ftState.WithFlag(StateFlags.SecurityMechanismDeactivated);
    }

    public static void InsertSignatureItems(this ReceiptResponse receiptResponse, List<SignatureItem> signaturItems)
    {
        receiptResponse.ftSignatures.InsertRange(0, signaturItems);
    }

    public static void AddSignatureItem(this ReceiptResponse receiptResponse, SignatureItem signaturItem)
    {
        receiptResponse.ftSignatures.Add(signaturItem);
    }

    public static void MarkAsFailed(this ReceiptResponse receiptResponse)
    {
        receiptResponse.ftState = receiptResponse.ftState.WithState(State.Error);
    }

    /// <summary>
    /// Whether a persisted receipt exists fiscally. True when <c>ftState</c> is not an error state, or when the persisted
    /// <c>PostFiscalization.FiscalizationSucceeded</c> is true: a receipt whose eInvoicing/eReporting finalize call failed
    /// (RFC 712) carries the error state although the SCU created a fiscal record. Every lookup that needs to know
    /// whether a persisted receipt is fiscalized must use this predicate instead of testing <c>ftState</c> directly.
    /// </summary>
    public static bool IsFiscalized(this ReceiptResponse receiptResponse)
    {
        if (!receiptResponse.ftState.IsState(State.Error))
        {
            return true;
        }

        try
        {
            return MiddlewareStateData.FromReceiptResponse(receiptResponse)?.PostFiscalization?.FiscalizationSucceeded == true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
