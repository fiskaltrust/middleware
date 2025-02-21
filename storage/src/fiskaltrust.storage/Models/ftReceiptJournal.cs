using System;

namespace fiskaltrust.storage.V0
{
    public class ftReceiptJournal
    {
        //PK
        public Guid ftReceiptJournalId { get; set; }
        public DateTime ftReceiptMoment { get; set; }
        public long ftReceiptNumber { get; set; }
        public decimal ftReceiptTotal { get; set; }

        //FK?
        public Guid ftQueueId { get; set; }

        //FK + UNIQUE INDEX
        public Guid ftQueueItemId { get; set; }

        //sha256(previouseReceiptJournalHash + ftReceiptJournalId + ftReceiptMoment.Ticks + ftReceiptNumber + requestHash + responseHash)
        public string ftReceiptHash { get; set; }

        //public string request { get; set; }
        //public string response { get; set; }

        public long TimeStamp { get; set; }
    }
}