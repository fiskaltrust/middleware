using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;

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
}