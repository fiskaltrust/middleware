using System;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

public static class ReceiptResponseHelper
{
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
        if (numSerieFactura.Count(x => x == '/') != 2)
        {
            throw new Exception("Invalid ftReceiptIdentification format. Needs exactly two '/'.");
        }
        var parts = numSerieFactura.Split('/');
        var serieFactura = $"{parts[0]}/{parts[1]}";
        if (!ulong.TryParse(parts[2], out ulong numFactura))
        {
            throw new Exception("Invalid ftReceiptIdentification format. Last part is not a number.");
        }
        return (serieFactura, numFactura);
    }
}
