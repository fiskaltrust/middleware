using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueIT : QueueLocalization, ICountrySpecificQueue
    {
        public Guid ftQueueId => ftQueueITId;

        public Guid? ftSignaturCreationUnitId => ftSignaturCreationUnitITId;

        public Guid ftQueueITId { get; set; }

        public Guid? ftSignaturCreationUnitITId { get; set; }

        public string LastHash { get; set; }

        public string CashBoxIdentification { get; set; }

        public int SSCDFailCount { get; set; }

        public DateTime? SSCDFailMoment { get; set; }

        public Guid? SSCDFailQueueItemId { get; set; }

        public int UsedFailedCount { get; set; }

        public DateTime? UsedFailedMomentMin { get; set; }

        public DateTime? UsedFailedMomentMax { get; set; }

        public Guid? UsedFailedQueueItemId { get; set; }
    }
}