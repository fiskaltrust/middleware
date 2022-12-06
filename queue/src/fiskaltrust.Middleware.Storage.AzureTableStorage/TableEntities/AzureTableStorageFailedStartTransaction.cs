using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFailedStartTransaction : BaseTableEntity
    {
        public string cbReceiptReference { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string CashBoxIdentification { get; set; }
        public string Request { get; set; }
        public DateTime StartMoment { get; set; }
    }
}
