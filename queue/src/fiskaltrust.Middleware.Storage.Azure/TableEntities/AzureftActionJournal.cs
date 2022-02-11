using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities
{
    public class AzureFtActionJournal : TableEntity
    {
        public Guid ftActionJournalId { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftQueueItemId { get; set; }
        public DateTime Moment { get; set; }
        public int Priority { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string DataBase64 { get; set; }
        public string DataJson { get; set; }
        public long TimeStamp { get; set; }
    }
}
