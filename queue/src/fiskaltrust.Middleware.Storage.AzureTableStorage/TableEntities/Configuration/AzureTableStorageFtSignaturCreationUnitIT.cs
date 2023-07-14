using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitIT : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitITId { get; set; }

        public string Url { get; set; }

        public long TimeStamp { get; set; }
        public string InfoJson { get; internal set; }
    }
}
