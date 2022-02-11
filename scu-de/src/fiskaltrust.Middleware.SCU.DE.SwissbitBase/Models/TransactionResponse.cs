using System;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Models
{
    public class TransactionResponse
    {
        public UInt64 LogTime { get; set; }
        //public string SerialNumberBase64 { get; set; }
        public byte[] SerialNumber { get; set; }
        public UInt64 SignatureCounter { get; set; }
        public string SignatureBase64 { get; set; }
        public UInt64 TransactionNumber { get; set; }
    }
}
