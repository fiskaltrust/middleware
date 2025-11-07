using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;

public static class ReceiptResponseHelpers
{
    public static void SetReceiptResponseErrored(this ReceiptResponse receiptResponse, string caption, string errorMessage)
    {
        receiptResponse.ftState = receiptResponse.ftState.WithState(State.Error);
        receiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = caption,
            Data = errorMessage,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureType.Unknown.WithCountry("BE").WithCategory(SignatureTypeCategory.Failure)
        });
    }

    public static void SetReceiptResponseErrored(this ReceiptResponse receiptResponse, string errorMessage)
    {
        receiptResponse.ftState = receiptResponse.ftState.WithState(State.Error);
        receiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "FAILURE",
            Data = errorMessage,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureType.Unknown.WithCountry("BE").WithCategory(SignatureTypeCategory.Failure)
        });
    }

    public static void AddSignatureItem(this ReceiptResponse receiptResponse, SignatureItem signaturItem)
    {
        var data = receiptResponse.ftSignatures.ToList();
        data.Add(signaturItem);
        receiptResponse.ftSignatures = data;
    }
}
