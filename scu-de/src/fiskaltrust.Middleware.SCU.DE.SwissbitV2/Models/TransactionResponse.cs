using System;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitV2.Models
{
    public class TransactionResponse
    {
        public ulong LogTime { get; set; }
        //public string SerialNumberBase64 { get; set; }
        public byte[] SerialNumber { get; set; }
        public ulong SignatureCounter { get; set; }
        public string SignatureBase64 { get; set; }
        public ulong TransactionNumber { get; set; }
    }
}
