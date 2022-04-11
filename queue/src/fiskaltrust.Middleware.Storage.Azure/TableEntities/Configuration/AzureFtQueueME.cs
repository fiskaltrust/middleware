using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtQueueME : TableEntity
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

        public long TimeStamp { get; set; }

        public int DailyClosingNumber { get; set; }

        public string IssuerTIN { get; set; }

        public string BusinUnitCode { get; set; }

        public string TCRIntID { get; set; }

        public string SoftCode { get; set; }

        public string MaintainerCode { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public string EnuType { get; set; }

        public string TCRCode { get; set; }
    }
}
