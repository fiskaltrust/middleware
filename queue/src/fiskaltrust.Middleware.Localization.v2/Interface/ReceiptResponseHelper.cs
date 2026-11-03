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
    /// Whether a persisted receipt exists fiscally. True when <c>ftState</c> is neither the error nor the fail state, or
    /// when the persisted <c>PostFiscalization.FiscalizationSucceeded</c> is true: a receipt whose eInvoicing/eReporting
    /// finalize call failed (RFC 712) carries the error state although the SCU created a fiscal record. This is the
    /// generalization of testing <c>!ftState.IsState(State.Error)</c>, which the receipt-reference lookups do; a lookup
    /// that tested <c>ftState.IsState(State.Success)</c> uses <see cref="IsSuccessfullyFiscalized"/> instead. Every lookup
    /// that needs to know whether a persisted receipt is fiscalized must use one of the two instead of testing
    /// <c>ftState</c> directly.
    /// </summary>
    public static bool IsFiscalized(this ReceiptResponse receiptResponse)
    {
        if (!receiptResponse.ftState.IsState(State.Error))
        {
            return !receiptResponse.ftState.IsState(State.Fail);
        }

        return HasFiscalizationSucceededMarker(receiptResponse);
    }

    /// <summary>
    /// Whether a persisted receipt was fiscalized with the plain success state: <c>ftState</c> is <see cref="State.Success"/>
    /// with no flag set, or it is the error state of a finalize failure (RFC 712) next to the FiscalizationSucceeded
    /// marker. This is the generalization of testing <c>ftState.IsState(State.Success)</c>, which the PT read paths
    /// (SAF-T export, document status, refund history) do: a receipt answered on a queue that is not operational carries
    /// the security-mechanism-deactivated flag and stays excluded, as it was before.
    /// </summary>
    public static bool IsSuccessfullyFiscalized(this ReceiptResponse receiptResponse)
        => receiptResponse.ftState.IsState(State.Success)
           || (receiptResponse.ftState.IsState(State.Error) && HasFiscalizationSucceededMarker(receiptResponse));

    private static bool HasFiscalizationSucceededMarker(ReceiptResponse receiptResponse)
    {
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
