using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureFailedFinishTransaction : TableEntity
    {
        public string cbReceiptReference { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime FinishMoment { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string Request { get; set; }
        public string CashBoxIdentification { get; set; }
    }
}
