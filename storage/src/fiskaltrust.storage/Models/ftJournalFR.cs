using System;

namespace fiskaltrust.storage.V0
{
    public class ftJournalFR
    {
        public Guid ftJournalFRId { get; set; }
        public string JWT { get; set; }
        public string JsonData { get; set; }
        public string ReceiptType { get; set; }
        public long Number { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}
