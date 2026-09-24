using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitPL : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitPLId { get; set; }
        public string Url { get; set; }
        public string InfoJson { get; set; }
        public long TimeStamp { get; set; }
    }
}
