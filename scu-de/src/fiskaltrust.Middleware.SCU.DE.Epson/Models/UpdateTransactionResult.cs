using System;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class UpdateTransactionResult
    {
        public DateTime LogTime { get; set; }
        public ulong SignatureCounter { get; set; }
        public string Signature { get; set; }
    }
}