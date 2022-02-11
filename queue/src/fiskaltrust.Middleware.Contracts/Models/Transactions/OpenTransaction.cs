using System;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Contracts.Models.Transactions
{
    public class OpenTransaction : TseTransaction
    {
        public long TransactionNumber { get; set; }
        public string StartTransactionSignatureBase64 { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
