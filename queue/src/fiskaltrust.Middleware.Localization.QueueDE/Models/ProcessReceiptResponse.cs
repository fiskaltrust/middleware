using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.Models
{
    public class ProcessReceiptResponse
    {
        public ulong TransactionNumber { get; set; }
        public List<SignaturItem> Signatures { get; set; }= new List<SignaturItem> { };
        public string ClientId { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string PublicKeyBase64 { get; set; }
        public string SerialNumberOctet { get; set; }
    }
}
