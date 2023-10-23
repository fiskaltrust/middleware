﻿using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalDE : BaseTableEntity
    {
        public Guid ftJournalDEId { get; set; }
        public long Number { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileContentBase64 { get; set; }
        public Guid ftQueueItemId { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}
