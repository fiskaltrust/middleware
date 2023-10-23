﻿using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitDE : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitDEId { get; set; }
        public string Url { get; set; }
        public string TseInfoJson { get; set; }
        public long TimeStamp { get; set; }
        public int Mode { get; set; }
        public string ModeConfigurationJson { get; set; }
    }
}
