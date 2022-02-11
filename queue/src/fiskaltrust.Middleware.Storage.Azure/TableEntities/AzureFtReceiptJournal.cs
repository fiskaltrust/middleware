using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureFtReceiptJournal : TableEntity
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
