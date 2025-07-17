using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtQueueES : BaseTableEntity
    {
        public Guid ftQueueESId { get; set; }
        public Guid ftSignaturCreationUnitESId { get; set; }
        public string LastHash { get; set; }
        public string CashBoxIdentification { get; set; }

        public Guid? SSCDSignQueueItemId { get; set; }

        public int SSCDFailCount { get; set; }
        public DateTime? SSCDFailMoment { get; set; }
        public Guid? SSCDFailQueueItemId { get; set; }


        public int UsedFailedCount { get; set; }
        public DateTime? UsedFailedMomentMin { get; set; }
        public DateTime? UsedFailedMomentMax { get; set; }
        public Guid? UsedFailedQueueItemId { get; set; }

        public long TimeStamp { get; set; }

        public int DailyClosingNumber { get; set; }
    }
}
