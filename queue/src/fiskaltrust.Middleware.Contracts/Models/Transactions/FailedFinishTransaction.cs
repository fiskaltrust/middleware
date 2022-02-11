using System;

namespace fiskaltrust.Middleware.Contracts.Models.Transactions
{
    public class FailedFinishTransaction : TseTransaction
    {
        public long? TransactionNumber { get; set; }
        public DateTime FinishMoment { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string Request { get; set; }
        public string CashBoxIdentification { get; set; }
    }
}
