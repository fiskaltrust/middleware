using System;

namespace fiskaltrust.storage.V0
{
    public enum JournalESType
    {
        VeriFactu,
        TicketBAI
    }

    public class ftJournalES
    {
        public Guid ftJournalESId { get; set; }
        public long Number { get; set; }
        public string RequestData { get; set; }
        public string ResponseData { get; set; }
        public string JournalType { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}