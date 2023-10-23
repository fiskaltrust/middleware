﻿using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalME : BaseTableEntity
    {
        public Guid ftJournalMEId { get; set; }
        public string cbReference { get; set; }
        public string InvoiceNumber { get; set; }
        public int YearlyOrdinalNumber { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
        public long JournalType { get; set; }
        public long Number { get; set; }
    }
}
