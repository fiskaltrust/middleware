using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureFailedStartTransaction : BaseTableEntity
    {
        public string cbReceiptReference { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string CashBoxIdentification { get; set; }
        public string Request { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
