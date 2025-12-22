using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtQueueGR : BaseTableEntity
    {
        public Guid ftQueueGRId { get; set; }
        public Guid? ftSignaturCreationUnitGRId { get; set; }
        public string LastHash { get; set; }
        public string LastSignature { get; set; }
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
