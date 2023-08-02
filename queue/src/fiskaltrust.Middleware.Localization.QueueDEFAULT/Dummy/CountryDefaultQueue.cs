using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy
{
    public class CountryDefaultQueue: ICountrySpecificQueue
    {
        public Guid ftQueueId { get; }
        public Guid? ftSignaturCreationUnitId { get; }
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
