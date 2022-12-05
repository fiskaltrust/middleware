using System;
using Azure.Data.Tables;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureFailedFinishTransaction : BaseTableEntity
    {
        public string cbReceiptReference { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime FinishMoment { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string Request { get; set; }
        public string CashBoxIdentification { get; set; }
    }
}
