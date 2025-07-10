using System;

namespace fiskaltrust.storage.V0
{
    public class ftJournalME
    {
        public Guid ftJournalMEId { get; set; }
        public string cbReference { get; set; } 
        public string InvoiceNumber { get; set; }
        public int YearlyOrdinalNumber { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long Number { get; set; }
        public long TimeStamp { get; set; }
        public string IIC { get; set; }
        public string FIC { get; set; }
        public string FCDC { get; set; }
        public long JournalType { get; set; }
    }
}