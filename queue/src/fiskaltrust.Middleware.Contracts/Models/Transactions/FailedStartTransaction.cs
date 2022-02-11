using System;

namespace fiskaltrust.Middleware.Contracts.Models.Transactions
{
    public class FailedStartTransaction : TseTransaction
    {
        public Guid ftQueueItemId { get; set; }
        public string CashBoxIdentification { get; set; }
        public string Request { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
