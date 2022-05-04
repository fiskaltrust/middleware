using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureFtJournalME : TableEntity
    {
        public Guid ftJournalMEId { get; set; }
        public string cbReference { get; set; }
        public string ftInvoiceNumber { get; set; }
        public int ftOrdinalNumber { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}
