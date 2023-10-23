﻿using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureTableStorageFtJournalAT : BaseTableEntity
    {
        public Guid ftJournalATId { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftSignaturCreationUnitId { get; set; }
        public long Number { get; set; }
        public string JWSHeaderBase64url { get; set; }
        public string JWSPayloadBase64url { get; set; }
        public string JWSSignatureBase64url { get; set; }
        public long TimeStamp { get; set; }
    }
}
