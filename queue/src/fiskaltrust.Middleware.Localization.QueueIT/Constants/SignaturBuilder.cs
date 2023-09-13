using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using System;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public static class SignaturBuilder
    {
        public static SignaturItem[] CreatePosReceiptCustomRTServerSignatures(ReceiptResponse response)
        {
            var stringBuilder = CreatePrintSignature01(response);
            var signatures = new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "[www.fiskaltrust.it]",
                    Data = stringBuilder.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954000000000001
                },
                new SignaturItem
                {
                    Caption = "DOCUMENTO COMMERCIALE",
                    Data = "di vendita o prestazione",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954000000000002
                }
            };
            return signatures.ToArray();
        }

        public static SignaturItem[] CreateRefundPosReceiptCustomRTServerSignatures(ReceiptResponse response)
        {
            var stringBuilder = CreatePrintSignature01(response);
            var stringBuilder02 = CreatePrintSignatureForVoidOrReso(response);
            var signatures = new List<SignaturItem>
        {
            new SignaturItem
            {
                Caption = "[www.fiskaltrust.it]",
                Data = stringBuilder.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000001
            },
            new SignaturItem
            {
                Caption = "DOCUMENTO COMMERCIALE",
                Data = stringBuilder02.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000002
            }
        };
            signatures.AddRange(response.ftSignatures);
            return signatures.ToArray();
        }

        private static StringBuilder CreatePrintSignature01(ReceiptResponse receiptResponse)
        {
            var receiptNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)?.Data);
            var zRepNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)?.Data);
            var rtDocumentMoment = DateTime.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data);
            var codiceLotteria = receiptResponse.GetSignaturItem(SignatureTypesIT.RTLotteryID)?.Data;
            var customerIdentification = receiptResponse.GetSignaturItem(SignatureTypesIT.RTCustomerID)?.Data;
            var shaMetadata = receiptResponse.GetSignaturItem(SignatureTypesIT.CustomRTServerShaMetadata)?.Data;
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

        private static StringBuilder CreatePrintSignatureForVoidOrReso(ReceiptResponse receiptResponse, string referencedRT = null, string referencedPrinterRT = null)
        {
            var docType = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentType)?.Data);
            var referenceZNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data);
            var referenceDocNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data);
            var referenceDateTime = DateTime.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data);
            var stringBuilder = new StringBuilder();
            if (docType == 3)
            {
                stringBuilder.AppendLine("emesso per RESO MERCE");
                stringBuilder.AppendLine($"N. {referenceZNumber.ToString().PadLeft(4, '0')}-{referenceDocNumber.ToString().PadLeft(4, '0')} del {referenceDateTime.ToString("dd-MM-yyyy")}");
                if (!string.IsNullOrEmpty(referencedRT))
                {
                    stringBuilder.AppendLine($"Server RT {referencedRT}");
                }
                if (!string.IsNullOrEmpty(referencedPrinterRT))
                {
                    stringBuilder.AppendLine($"RT {referencedRT}");
                }
                stringBuilder.AppendLine($"Cassa {receiptResponse.ftCashBoxIdentification}");
            }
            else if (docType == 5)
            {
                stringBuilder.AppendLine("emesso per ANNULLAMENTO");
                stringBuilder.AppendLine($"N. {referenceZNumber.ToString().PadLeft(4, '0')}-{referenceDocNumber.ToString().PadLeft(4, '0')} del {referenceDateTime.ToString("dd-MM-yyyy")}");
                if (!string.IsNullOrEmpty(referencedRT))
                {
                    stringBuilder.AppendLine($"Server RT {referencedRT}");
                }
                if (!string.IsNullOrEmpty(referencedPrinterRT))
                {
                    stringBuilder.AppendLine($"RT {referencedRT}");
                }
                stringBuilder.AppendLine($"Cassa {receiptResponse.ftCashBoxIdentification}");
            }
            return stringBuilder;
        }
    }
}
