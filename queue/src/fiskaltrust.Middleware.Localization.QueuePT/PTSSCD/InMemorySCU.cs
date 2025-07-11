using System.Security.Cryptography;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;

public class PTSSCDInfo
{
}

public class InMemorySCUConfiguration
{

}

public class InMemorySCU : IPTSSCD
{
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT;

    public InMemorySCU(ftSignaturCreationUnitPT signaturCreationUnitPT)
    {
        _signaturCreationUnitPT = signaturCreationUnitPT;
    }

    public PTInvoiceElement GetPTInvoiceElementFromReceiptRequest(ReceiptRequest receipt, string invoiceNo, string? lastHash)
    {
        return new PTInvoiceElement
        {
            InvoiceDate = receipt.cbReceiptMoment,
            SystemEntryDate = DateTime.UtcNow, // wrong
            InvoiceNo = invoiceNo, // wrong
            GrossTotal = receipt.cbChargeItems.Sum(x => x.Amount),
            Hash = lastHash ?? ""
        };
    }

    public string GetHashForItem(PTInvoiceElement element)
    {
        return $"{element.InvoiceDate:yyyy-MM-dd};" +
               $"{element.SystemEntryDate:yyyy-MM-ddTHH:mm:ss};" +
               $"{element.InvoiceNo};" +
               $"{element.GrossTotal:0.00};" +
               $"{element.Hash}";
    }

#pragma warning disable
    public async Task<(ProcessResponse, string)> ProcessReceiptAsync(ProcessRequest request, string invoiceNo, string? lastHash)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_signaturCreationUnitPT.PrivateKey);
        var hash1 = GetHashForItem(GetPTInvoiceElementFromReceiptRequest(request.ReceiptRequest, invoiceNo, lastHash ?? ""));
        var signature1 = rsa.SignData(Encoding.UTF8.GetBytes(hash1), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return await Task.FromResult((new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse,
        }, Convert.ToBase64String(signature1)));
    }

    public Task<PTSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
