using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueME : QueueLocalization
    {
        public Guid ftQueueMEId { get; set; }

        public Guid? ftSignaturCreationUnitMEId { get; set; }

        public string LastHash { get; set; }

        public int SSCDFailCount { get; set; }

        public DateTime? SSCDFailMoment { get; set; }

        public Guid? SSCDFailQueueItemId { get; set; }

        public int UsedFailedCount { get; set; }

        public DateTime? UsedFailedMomentMin { get; set; }

        public DateTime? UsedFailedMomentMax { get; set; }

        public Guid? UsedFailedQueueItemId { get; set; }

        public int DailyClosingNumber { get; set; }

    }
}