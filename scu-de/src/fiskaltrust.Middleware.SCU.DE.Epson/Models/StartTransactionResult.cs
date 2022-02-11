using System;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class StartTransactionResult
    {
        public ulong TransactionNumber { get; set; }
        public DateTime LogTime { get; set; }
        public string SerialNumber { get; set; }
        public ulong SignatureCounter { get; set; }
        public string Signature { get; set; }
    }
}