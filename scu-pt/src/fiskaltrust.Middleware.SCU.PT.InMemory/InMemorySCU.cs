using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.SCU.PT.Abstraction;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.PT.InMemory;

public class InMemorySCU : IPTSSCD
{
    private string _privateKey;

    public InMemorySCU(string privateKey)
    {
        _privateKey = privateKey;
    }
    public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
    {
        return await Task.FromResult(new EchoResponse { Message = echoRequest.Message });
    }
    public async Task<PTSSCDInfo> GetInfoAsync()
    {
        return await Task.FromResult(new PTSSCDInfo());
    }
    public PTInvoiceElement GetPTInvoiceElementFromReceiptRequest(ReceiptRequest receipt, ReceiptResponse receiptResponse, string lastHash)
    {
        var element = new PTInvoiceElement
        {
            InvoiceDate = receipt.cbReceiptMoment,
            SystemEntryDate = receipt.cbReceiptMoment,
            InvoiceNo = receiptResponse.ftReceiptIdentification.Split("#").Last(),
            GrossTotal = receipt.cbChargeItems.Sum(x => receipt.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) || x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? -x.Amount : x.Amount),
            Hash = lastHash ?? ""
        };
        // Todo this is just a workoaurnd while we are going through the certification
        if (receipt.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            element.SystemEntryDate = receiptResponse.ftReceiptMoment;
        }
        return element;
    }

    public string GetHashForItem(PTInvoiceElement element)
    {
        return $"{element.InvoiceDate:yyyy-MM-dd};" +
               $"{element.SystemEntryDate:yyyy-MM-ddTHH:mm:ss};" +
               $"{element.InvoiceNo};" +
               $"{element.GrossTotal.ToString("0.00", CultureInfo.InvariantCulture)};" +
               $"{element.Hash}";
    }

    public async Task<(ProcessResponse, string)> ProcessReceiptAsync(ProcessRequest request, string? lastHash)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKey);
        var hash1 = GetHashForItem(GetPTInvoiceElementFromReceiptRequest(request.ReceiptRequest, request.ReceiptResponse, lastHash ?? ""));
        var signature1 = rsa.SignData(Encoding.UTF8.GetBytes(hash1), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return await Task.FromResult((new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse,
        }, Convert.ToBase64String(signature1)));
    }

    public string SignData(string hash1)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKey);
        var signature1 = rsa.SignData(Encoding.ASCII.GetBytes(hash1), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature1);
    }
}
