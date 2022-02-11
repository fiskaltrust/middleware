using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class FinishTransactionEndResult
    {
        public DateTime LogTime { get; set; }
        public ulong SignatureCounter { get; set; }
        public string SignatureBase64 { get; set; }
    }
}