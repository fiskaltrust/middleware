using System;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

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

    public static void AddSignatureItem(this ReceiptResponse receiptResponse, SignatureItem signaturItem)
    {
        receiptResponse.ftSignatures.Add(signaturItem);
    }

    public static string GetNumSerieFactura(this ReceiptResponse receiptResponse)
    {
        if (receiptResponse.ftReceiptIdentification.Count(x => x == '#') != 1)
        {
            throw new Exception("Invalid ftReceiptIdentification format. Needs exactly one '#'.");
        }
        return receiptResponse.ftReceiptIdentification.Split('#')[1];
    }

    public static (string serieFactura, ulong numFactura) GetNumSerieFacturaParts(this ReceiptResponse receiptResponse)
    {
        var numSerieFactura = receiptResponse.GetNumSerieFactura();
        
        // Find the last occurrence of either '/' or '-' separator
        int lastSlashIndex = numSerieFactura.LastIndexOf('/');
        int lastDashIndex = numSerieFactura.LastIndexOf('-');
        int lastSeparatorIndex = Math.Max(lastSlashIndex, lastDashIndex);
        
        if (lastSeparatorIndex == -1)
        {
            throw new Exception("Invalid ftReceiptIdentification format. Needs at least one '/' or '-' separator.");
        }
        
        // Split at the last separator
        var serieFactura = numSerieFactura.Substring(0, lastSeparatorIndex);
        var numFacturaStr = numSerieFactura.Substring(lastSeparatorIndex + 1);
        
        if (!ulong.TryParse(numFacturaStr, out ulong numFactura))
        {
            throw new Exception("Invalid ftReceiptIdentification format. Last part after separator is not a valid number.");
        }
        
        return (serieFactura, numFactura);
    }
}
