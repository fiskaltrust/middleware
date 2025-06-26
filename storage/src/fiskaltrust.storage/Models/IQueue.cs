using System;

namespace fiskaltrust.storage.V0
{
    public interface ICountrySpecificQueue
    {
        Guid ftQueueId { get; }

        Guid? ftSignaturCreationUnitId { get;}

        string LastHash { get; set; }

        string CashBoxIdentification { get; set; }

        int SSCDFailCount { get; set; }

        DateTime? SSCDFailMoment { get; set; }

        Guid? SSCDFailQueueItemId { get; set; }

        int UsedFailedCount { get; set; }

        DateTime? UsedFailedMomentMin { get; set; }

        DateTime? UsedFailedMomentMax { get; set; }

        Guid? UsedFailedQueueItemId { get; set; }
    }
}
