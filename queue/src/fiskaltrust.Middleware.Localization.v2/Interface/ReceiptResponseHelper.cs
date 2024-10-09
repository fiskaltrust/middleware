using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public static class ReceiptResponseHelper
{
    public static void SetReceiptResponseError(this ReceiptResponse receiptResponse, string errorMessage)
    {
        receiptResponse.ftState |= 0xEEEE_EEEE;
        receiptResponse.ftSignatures = [];
        receiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "FAILURE",
            Data = errorMessage,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            ftSignatureType = (long) ((ulong) receiptResponse.ftState & (ulong) 0xFFFF_F000_0000_0000) | 0x3000
        });
    }

    public static void MarkAsDisabled(this ReceiptResponse receiptResponse)
    {
        receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
    }

    public static void InsertSignatureItems(this ReceiptResponse receiptResponse, List<SignatureItem> signaturItems)
    {
        receiptResponse.ftSignatures.InsertRange(0, signaturItems);
    }

    public static void AddSignatureItem(this ReceiptResponse receiptResponse, SignatureItem signaturItem)
    {
        receiptResponse.ftSignatures.Add(signaturItem);
    }

    public static bool HasFailed(this ReceiptResponse receiptRespons) => (receiptRespons.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE;
}
