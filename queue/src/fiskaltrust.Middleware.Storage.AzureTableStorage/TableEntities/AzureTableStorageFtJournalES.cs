using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalES : BaseTableEntity
    {
        public Guid ftJournalESId { get; set; }

        public Guid ftSignaturCreationUnitId { get; set; }

        public Guid ftQueueId { get; set; }

        public string JournalType { get; set; }

        public byte[] JournalData { get; set; }

        public long TimeStamp { get; set; }
    }
}
