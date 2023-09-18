using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public static class SignaturBuilder
    {
        public static List<SignaturItem> CreatePOSReceiptFormatSignatures(ReceiptResponse response)
        {
            return new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "[www.fiskaltrust.it]",
                    Data = CreateFooter(response).ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.PosReceiptPrimarySignature
                },
                new SignaturItem
                {
                    Caption = "DOCUMENTO COMMERCIALE",
                    Data = CreateHeader(response).ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.PosReceiptSecondarySignature
                }
            };
        }

        private static StringBuilder CreateFooter(ReceiptResponse receiptResponse)
        {
            var receiptNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)?.Data);
            var zRepNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)?.Data);
            var rtDocumentMoment = DateTime.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data);
            var codiceLotteria = receiptResponse.GetSignaturItem(SignatureTypesIT.RTLotteryID)?.Data;
            var customerIdentification = receiptResponse.GetSignaturItem(SignatureTypesIT.RTCustomerID)?.Data;
            var shaMetadata = receiptResponse.GetSignaturItem(SignatureTypesIT.RTServerShaMetadata)?.Data;
            var rtServerSerialNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTSerialNumber)?.Data;

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{rtDocumentMoment.ToString("dd-MM-yyyy HH:mm")}");
            stringBuilder.AppendLine($"DOCUMENTO N. {zRepNumber.ToString().PadLeft(4, '0')}-{receiptNumber.ToString().PadLeft(4, '0')}");
            if (!string.IsNullOrEmpty(codiceLotteria))
            {
                stringBuilder.AppendLine($"Codice Lotteria: {codiceLotteria}");
                stringBuilder.AppendLine();
            }
            if (!string.IsNullOrEmpty(customerIdentification))
            {
                stringBuilder.AppendLine($"Codice Fiscale: {customerIdentification}");
            }
            if (!string.IsNullOrEmpty(shaMetadata))
            {
                stringBuilder.AppendLine($"Server RT {rtServerSerialNumber}");
            }
            stringBuilder.AppendLine($"Cassa {receiptResponse.ftCashBoxIdentification}");
            if (!string.IsNullOrEmpty(shaMetadata))
            {
                stringBuilder.AppendLine($"-----FIRMA ELETTRONICA-----");
                stringBuilder.AppendLine(shaMetadata);
                stringBuilder.AppendLine("---------------------------");
            }
            return stringBuilder;
        }

        private static StringBuilder CreateHeader(ReceiptResponse receiptResponse, string referencedRT = null, string referencedPrinterRT = null)
        {
            var docType = receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentType)?.Data;
            if (docType.ToUpper() == "POSRECEIPT")
            {
                return new StringBuilder("di vendita o prestazione");
            }

            var referenceZNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
            var referenceDocNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
            var referenceDateTimeString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
            var stringBuilder = new StringBuilder();
            if (docType.ToUpper() == "REFUND")
            {
                stringBuilder.AppendLine("emesso per RESO MERCE");
                if (string.IsNullOrEmpty(referenceZNumberString) || string.IsNullOrEmpty(referenceDocNumberString))
                {
                    stringBuilder.AppendLine($"NP del {DateTime.Parse(referenceDateTimeString).ToString("dd-MM-yyyy")}");
                }
                else
                {
                    stringBuilder.AppendLine($"N. {long.Parse(referenceZNumberString).ToString().PadLeft(4, '0')}-{long.Parse(referenceDocNumberString).ToString().PadLeft(4, '0')} del {DateTime.Parse(referenceDateTimeString).ToString("dd-MM-yyyy")}");
                }
                if (!string.IsNullOrEmpty(referencedRT))
                {
                    stringBuilder.AppendLine($"Server RT {referencedRT}");
                }
                if (!string.IsNullOrEmpty(referencedPrinterRT))
                {
                    stringBuilder.AppendLine($"RT {referencedRT}");
                }
            }
            else if (docType.ToUpper() == "VOID")
            {
                stringBuilder.AppendLine("emesso per ANNULLAMENTO");
                if (string.IsNullOrEmpty(referenceZNumberString) || string.IsNullOrEmpty(referenceDocNumberString))
                {
                    stringBuilder.AppendLine($"POS del {DateTime.Parse(referenceDateTimeString).ToString("dd-MM-yyyy")}");
                }
                else
                {
                    stringBuilder.AppendLine($"N. {long.Parse(referenceZNumberString).ToString().PadLeft(4, '0')}-{long.Parse(referenceDocNumberString).ToString().PadLeft(4, '0')} del {DateTime.Parse(referenceDateTimeString).ToString("dd-MM-yyyy")}");
                }
                if (!string.IsNullOrEmpty(referencedRT))
                {
                    stringBuilder.AppendLine($"Server RT {referencedRT}");
                }
                if (!string.IsNullOrEmpty(referencedPrinterRT))
                {
                    stringBuilder.AppendLine($"RT {referencedRT}");
                }
            }
            return stringBuilder;
        }
    }
}
