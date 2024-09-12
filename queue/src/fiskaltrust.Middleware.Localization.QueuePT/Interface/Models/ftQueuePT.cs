using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Interface.Models
{
    public class ftQueuePT : QueueLocalization, ICountrySpecificQueue
    {
        public Guid ftQueueId => ftQueuePTId;

        public Guid? ftSignaturCreationUnitId => ftSignaturCreationUnitPTId;

        public Guid ftQueuePTId { get; set; }

        public Guid? ftSignaturCreationUnitPTId { get; set; }

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
