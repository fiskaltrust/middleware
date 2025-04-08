using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction
{
    public static class ReceiptResponseHelper
    {
        public static void SetReceiptResponseErrored(this ReceiptResponse receiptResponse, string caption, string errorMessage)
        {
            receiptResponse.ftState |= 0xEEEE_EEEE;
            receiptResponse.ftSignatures = new List<SignaturItem>().ToArray();
            receiptResponse.AddSignatureItem(new SignaturItem
            {
                Caption = caption,
                Data = errorMessage,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954_2000_0000_3000
            });
        }

        public static void SetReceiptResponseErrored(this ReceiptResponse receiptResponse, string errorMessage)
        {
            receiptResponse.ftState |= 0xEEEE_EEEE;
            receiptResponse.ftSignatures = new List<SignaturItem>().ToArray();
            receiptResponse.AddSignatureItem(new SignaturItem
            {
                Caption = "FAILURE",
                Data = errorMessage,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954_2000_0000_3000
            });
        }

        public static void AddWarningSignatureItem(this ReceiptResponse receiptResponse, string warning)
        {
            var data = receiptResponse.ftSignatures.ToList();
            data.Add(new SignaturItem()
            {
                Caption = "WARNING",
                Data = warning ?? "",
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954_2000_0020_1000
            });
            receiptResponse.ftSignatures = data.ToArray();
        }

        public static void AddSignatureItem(this ReceiptResponse receiptResponse, SignaturItem signaturItem)
        {
            var data = receiptResponse.ftSignatures.ToList();
            data.Add(signaturItem);
            receiptResponse.ftSignatures = data.ToArray();
        }

        public static bool HasFailed(this ReceiptResponse receiptRespons) => (receiptRespons.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE;

        public static SignaturItem? GetSignaturItem(this ReceiptResponse receiptResponse, SignatureTypesIT signatureTypesIT) => receiptResponse.ftSignatures.FirstOrDefault(x => (x.ftSignatureType & 0xFF) == (long) signatureTypesIT);
    }
}
