﻿using System;

namespace fiskaltrust.storage.V0
{
    public class ftJournalIT
    {
        public Guid ftJournalITId { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftSignaturCreationUnitITId { get; set; }
        public long ReceiptNumber { get; set; }
        public long ZRepNumber { get; set; }
        public long JournalType { get; set; }
        public string cbReceiptReference { get; set; }
        public string DataJson { get; set; }
        public DateTime ReceiptDateTime { get; set; }
        public long TimeStamp { get; set; }
    }
}