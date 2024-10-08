using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.PTSSCD
{
    public class PTSSCDInfo
    {
    }

    public interface IPTSSCD
    {
        Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string lastHash);

        Task<PTSSCDInfo> GetInfoAsync();
    }

    public class InMemorySCUConfiguration
    {
        public string? PrivateKey { get; set; }
    }

    public class InMemorySCU : IPTSSCD
    {
        private readonly InMemorySCUConfiguration _configuration;

        public InMemorySCU(Dictionary<string, object> scuConfiguration)
        {
            _configuration = JsonConvert.DeserializeObject<InMemorySCUConfiguration>(JsonConvert.SerializeObject(scuConfiguration));
        }

        public InMemorySCU(InMemorySCUConfiguration scuConfiguration)
        {
            _configuration = scuConfiguration;
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
            rsa.ImportFromPem(_configuration.PrivateKey);
            var hash1 = GetHashForItem(GetPTInvoiceElementFromReceiptRequest(request.ReceiptRequest, lastHash));
            var signature1 = rsa.SignData(Encoding.UTF8.GetBytes(hash1), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            return await Task.FromResult((new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse,
            }, Convert.ToBase64String(signature1)));
        }

        public Task<PTSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
    }
}
