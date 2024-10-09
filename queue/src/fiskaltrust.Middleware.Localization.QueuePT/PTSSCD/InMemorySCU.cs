﻿using System.Security.Cryptography;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Storage.PT;
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

    public PTInvoiceElement GetPTInvoiceElementFromReceiptRequest(ReceiptRequest receipt, string lastHash)
    {
        return new PTInvoiceElement
        {
            InvoiceDate = receipt.cbReceiptMoment,
            SystemEntryDate = receipt.cbReceiptMoment, // wrong
            InvoiceNo = receipt.cbReceiptReference, // wrong
            GrossTotal = receipt.cbChargeItems.Sum(x => x.Amount),
            Hash = lastHash
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

    public async Task<(ProcessResponse, string)> ProcessReceiptAsync(ProcessRequest request, string lastHash)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_signaturCreationUnitPT.PrivateKey);
        var hash1 = GetHashForItem(GetPTInvoiceElementFromReceiptRequest(request.ReceiptRequest, lastHash));
        var signature1 = rsa.SignData(Encoding.UTF8.GetBytes(hash1), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return await Task.FromResult((new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse,
        }, Convert.ToBase64String(signature1)));
    }

    public Task<PTSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
