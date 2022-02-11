using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public class UpdateTransactionEndResult
    {
        public DateTime LogTime { get; set; }
        public ulong SignatureCounter { get; set; }
        public string SignatureBase64 { get; set; }
    }
}