using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalIT : BaseTableEntity
    {
        public Guid ftJournalITId { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftSignaturCreationUnitITId { get; set; }
        public int RecNumber { get; set; }
        public int ZRecNumber { get; set; }
        public long JournalType { get; set; }
        public string cbReceiptReference { get; set; }
        public string RecordDataJson { get; set; }
        public DateTime RecDate { get; set; }
        public long TimeStamp { get; set; }
    }
}
