using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtQueue : BaseTableEntity
    {
        public Guid ftQueueId { get; set; }
        public Guid ftCashBoxId { get; set; }
        public long ftCurrentRow { get; set; }
        public long ftQueuedRow { get; set; }
        public long ftReceiptNumerator { get; set; }
        public double ftReceiptTotalizer { get; set; }
        public string ftReceiptHash { get; set; }
        public DateTime? StartMoment { get; set; }
        public DateTime? StopMoment { get; set; }
        public string CountryCode { get; set; }
        public int Timeout { get; set; }
        public long TimeStamp { get; set; }
    }
}
