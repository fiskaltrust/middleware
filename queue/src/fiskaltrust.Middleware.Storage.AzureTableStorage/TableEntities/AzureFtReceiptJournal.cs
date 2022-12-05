using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureFtReceiptJournal : BaseTableEntity
    {
        public Guid ftReceiptJournalId { get; set; }
        public DateTime ftReceiptMoment { get; set; }
        public long ftReceiptNumber { get; set; }
        public double ftReceiptTotal { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string ftReceiptHash { get; set; }
        public long TimeStamp { get; set; }
    }
}
