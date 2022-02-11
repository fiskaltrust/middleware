using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class StartTransactionEndResult
    {
        public DateTime LogTime { get; set; }
        public ulong TransactionNo { get; set; }
        public string SerialNoBase64 { get; set; }
        public ulong SignatureCounter { get; set; }
        public string SignatureBase64 { get; set; }
    }
}