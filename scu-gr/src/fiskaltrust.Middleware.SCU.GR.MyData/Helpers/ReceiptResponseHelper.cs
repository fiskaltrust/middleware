using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;

namespace fiskaltrust.Middleware.SCU.GR.MyData.Helpers;

public static class ReceiptResponseHelper
{
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

    /// <summary>
    /// Retypes the FAILURE signature as SignatureTypeGR.DuplicateInvoiceError, keeping
    /// its category and flags. Called when AADE rejected a SendInvoices submission with
    /// validation error 233 (an invoice with this numbering is already transmitted):
    /// QueueGR's counter reservation recognizes the duplicate by this signature type —
    /// "number consumed, advance" — instead of parsing the serialized error payload.
    /// </summary>
    public static void MarkDuplicateInvoiceFailure(this ReceiptResponse receiptResponse)
    {
        foreach (var signature in receiptResponse.ftSignatures)
        {
            if (string.Equals(signature.Caption, "FAILURE", StringComparison.Ordinal))
            {
                signature.ftSignatureType = signature.ftSignatureType.WithType(SignatureTypeGR.DuplicateInvoiceError);
            }
        }
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

    public static bool HasFailed(this ReceiptResponse receiptRespons) => receiptRespons.ftState.IsState(State.Error);
}
