using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtQueuePL : BaseTableEntity
    {
        public Guid ftQueuePLId { get; set; }
        public Guid? ftSignaturCreationUnitPLId { get; set; }
        public string CashBoxIdentification { get; set; }

        public int SSCDFailCount { get; set; }
        public DateTime? SSCDFailMoment { get; set; }
        public Guid? SSCDFailQueueItemId { get; set; }

        public int UsedFailedCount { get; set; }
        public DateTime? UsedFailedMomentMin { get; set; }
        public DateTime? UsedFailedMomentMax { get; set; }
        public Guid? UsedFailedQueueItemId { get; set; }

        public long TimeStamp { get; set; }
    }
}
