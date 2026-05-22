using System.Text;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueueIT queueIT, RTInfo rtInfo) => new()
    {
        ftSignatureType = (SignatureType) (Cases.BASE_STATE | 0x1_1001),
        ftSignatureFormat = SignatureFormat.Text,
        Caption = "Initial-operation receipt",
        Data = $"Queue-ID: {queueIT.ftQueueITId} Serial-Nr: {rtInfo.SerialNumber}",
    };

    public static SignatureItem CreateOutOfOperationSignature(ftQueueIT queueIT) => new()
    {
        ftSignatureType = (SignatureType) (Cases.BASE_STATE | 0x1_1002),
        ftSignatureFormat = SignatureFormat.Text,
        Caption = "Out-of-operation receipt",
        Data = $"Queue-ID: {queueIT.ftQueueITId}",
    };

    public static List<SignatureItem> CreatePOSReceiptFormatSignatures(ReceiptResponse response) => new()
    {
        new SignatureItem
        {
            Caption = "[www.fiskaltrust.it]",
            Data = CreateFooter(response).ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.PosReceiptPrimarySignature),
        },
        new SignatureItem
        {
            Caption = "DOCUMENTO COMMERCIALE",
            Data = CreateHeader(response).ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.PosReceiptSecondarySignature),
        },
    };

    private static StringBuilder CreateFooter(ReceiptResponse receiptResponse)
    {
        var receiptNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)!.Data);
        var zRepNumber = long.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data);
        var rtDocumentMoment = DateTime.Parse(receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)!.Data);
        var codiceLotteria = receiptResponse.GetSignaturItem(SignatureTypesIT.RTLotteryID)?.Data;
        var customerIdentification = receiptResponse.GetSignaturItem(SignatureTypesIT.RTCustomerID)?.Data;
        var shaMetadata = receiptResponse.GetSignaturItem(SignatureTypesIT.RTServerShaMetadata)?.Data;
        var rtServerSerialNumber = receiptResponse.GetSignaturItem(SignatureTypesIT.RTSerialNumber)?.Data;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(rtDocumentMoment.ToString("dd-MM-yyyy HH:mm"));
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
            stringBuilder.AppendLine("-----FIRMA ELETTRONICA-----");
            stringBuilder.AppendLine(shaMetadata);
            stringBuilder.AppendLine("---------------------------");
        }
        return stringBuilder;
    }

    private static StringBuilder CreateHeader(ReceiptResponse receiptResponse, string? referencedRT = null, string? referencedPrinterRT = null)
    {
        var docType = receiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentType)?.Data;
        if (string.Equals(docType, "POSRECEIPT", StringComparison.OrdinalIgnoreCase))
        {
            return new StringBuilder("di vendita o prestazione");
        }

        var referenceZNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        var stringBuilder = new StringBuilder();
        if (string.Equals(docType, "REFUND", StringComparison.OrdinalIgnoreCase))
        {
            stringBuilder.AppendLine("emesso per RESO MERCE");
            AppendReference(stringBuilder, referenceZNumberString, referenceDocNumberString, referenceDateTimeString);
            AppendRtIdentifiers(stringBuilder, referencedRT, referencedPrinterRT);
        }
        else if (string.Equals(docType, "VOID", StringComparison.OrdinalIgnoreCase))
        {
            stringBuilder.AppendLine("emesso per ANNULLAMENTO");
            AppendReference(stringBuilder, referenceZNumberString, referenceDocNumberString, referenceDateTimeString);
            AppendRtIdentifiers(stringBuilder, referencedRT, referencedPrinterRT);
        }
        return stringBuilder;
    }

    private static void AppendReference(StringBuilder sb, string? referenceZNumber, string? referenceDocNumber, string? referenceDateTime)
    {
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber))
        {
            sb.AppendLine($"ND del {DateTime.Parse(referenceDateTime!).ToString("dd-MM-yyyy")}");
        }
        else
        {
            sb.AppendLine($"N. {long.Parse(referenceZNumber).ToString().PadLeft(4, '0')}-{long.Parse(referenceDocNumber).ToString().PadLeft(4, '0')} del {DateTime.Parse(referenceDateTime!).ToString("dd-MM-yyyy")}");
        }
    }

    private static void AppendRtIdentifiers(StringBuilder sb, string? referencedRT, string? referencedPrinterRT)
    {
        if (!string.IsNullOrEmpty(referencedRT))
        {
            sb.AppendLine($"Server RT {referencedRT}");
        }
        if (!string.IsNullOrEmpty(referencedPrinterRT))
        {
            sb.AppendLine($"RT {referencedRT}");
        }
    }
}
