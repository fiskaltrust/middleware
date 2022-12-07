using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalFR : BaseTableEntity
    {
        public Guid ftJournalFRId { get; set; }
        public string JWT { get; set; }
        public string JsonData { get; set; }
        public string ReceiptType { get; set; }
        public long Number { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}
