using System.Text;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueueIT queueIT, RTInfo rtInfo)
    {
        return new SignatureItem
        {
            ftSignatureType = SignatureTypeIT.InitialOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = "Initial-operation receipt",
            Data = $"Queue-ID: {queueIT.ftQueueITId} Serial-Nr: {rtInfo.SerialNumber}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueueIT queueIT)
    {
        return new SignatureItem
        {
            ftSignatureType = SignatureTypeIT.OutOfOperationReceipt.As<SignatureType>().WithFlag(SignatureTypeFlags.ArchivingRequired),
            ftSignatureFormat = SignatureFormat.Text,
            Caption = "Out-of-operation receipt",
            Data = $"Queue-ID: {queueIT.ftQueueITId}"
        };
    }

    /// <summary>
    /// The printable header and footer of the "documento commerciale", built from the RT signatures the SCU returned.
    /// </summary>
    public static List<SignatureItem> CreatePOSReceiptFormatSignatures(ReceiptResponse response)
    {
        return
        [
            new SignatureItem
            {
                Caption = "[www.fiskaltrust.it]",
                Data = CreateFooter(response).ToString(),
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeIT.PosReceiptPrimarySignature.As<SignatureType>()
            },
            new SignatureItem
            {
                Caption = "DOCUMENTO COMMERCIALE",
                Data = CreateHeader(response).ToString(),
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeIT.PosReceiptSecondarySignature.As<SignatureType>()
            }
        ];
    }

    private static StringBuilder CreateFooter(ReceiptResponse receiptResponse)
    {
        var receiptNumber = long.Parse(receiptResponse.GetSignatureItem(SignatureTypeIT.RTDocumentNumber)!.Data);
        var zRepNumber = long.Parse(receiptResponse.GetSignatureItem(SignatureTypeIT.RTZNumber)!.Data);
        var rtDocumentMoment = DateTime.Parse(receiptResponse.GetSignatureItem(SignatureTypeIT.RTDocumentMoment)!.Data);
        var codiceLotteria = receiptResponse.GetSignatureItem(SignatureTypeIT.RTLotteryID)?.Data;
        var customerIdentification = receiptResponse.GetSignatureItem(SignatureTypeIT.RTCustomerID)?.Data;
        var shaMetadata = receiptResponse.GetSignatureItem(SignatureTypeIT.RTServerShaMetadata)?.Data;
        var rtServerSerialNumber = receiptResponse.GetSignatureItem(SignatureTypeIT.RTSerialNumber)?.Data;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"{rtDocumentMoment:dd-MM-yyyy HH:mm}");
        stringBuilder.AppendLine($"DOCUMENTO N. {zRepNumber.ToString().PadLeft(4, '0')}-{receiptNumber.ToString().PadLeft(4, '0')}");
        if (!string.IsNullOrEmpty(codiceLotteria))
        {
            stringBuilder.AppendLine($"Codice Lotteria: {codiceLotteria}");
            stringBuilder.AppendLine();
        }
        if (!string.IsNullOrEmpty(customerIdentification))
        {
            stringBuilder.AppendLine($"Cod. Fisc./P.IVA: {customerIdentification}");
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

    private static StringBuilder CreateHeader(ReceiptResponse receiptResponse, string? referencedRT = null, string? referencedPrinterRT = null)
    {
        var docType = receiptResponse.GetSignatureItem(SignatureTypeIT.RTDocumentType)?.Data ?? "";
        if (docType.ToUpper() == "POSRECEIPT")
        {
            return new StringBuilder("di vendita o prestazione");
        }

        var referenceZNumberString = receiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceZNumber)?.Data;
        var referenceDocNumberString = receiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = receiptResponse.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentMoment)?.Data;
        var stringBuilder = new StringBuilder();
        if (docType.ToUpper() == "REFUND")
        {
            stringBuilder.AppendLine("emesso per RESO MERCE");
            AppendReference(stringBuilder, referenceZNumberString, referenceDocNumberString, referenceDateTimeString, referencedRT, referencedPrinterRT);
        }
        else if (docType.ToUpper() == "VOID")
        {
            stringBuilder.AppendLine("emesso per ANNULLAMENTO");
            AppendReference(stringBuilder, referenceZNumberString, referenceDocNumberString, referenceDateTimeString, referencedRT, referencedPrinterRT);
        }
        return stringBuilder;
    }

    private static void AppendReference(StringBuilder stringBuilder, string? referenceZNumberString, string? referenceDocNumberString, string? referenceDateTimeString, string? referencedRT, string? referencedPrinterRT)
    {
        if (string.IsNullOrEmpty(referenceZNumberString) || string.IsNullOrEmpty(referenceDocNumberString))
        {
            stringBuilder.AppendLine(string.IsNullOrEmpty(referenceDateTimeString)
                ? "ND"
                : $"ND del {DateTime.Parse(referenceDateTimeString):dd-MM-yyyy}");
        }
        else
        {
            stringBuilder.AppendLine($"N. {long.Parse(referenceZNumberString).ToString().PadLeft(4, '0')}-{long.Parse(referenceDocNumberString).ToString().PadLeft(4, '0')} del {DateTime.Parse(referenceDateTimeString!):dd-MM-yyyy}");
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
}
