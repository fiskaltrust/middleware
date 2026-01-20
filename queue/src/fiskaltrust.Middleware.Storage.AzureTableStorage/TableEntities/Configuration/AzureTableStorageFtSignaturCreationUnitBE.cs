using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitBE : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitBEId { get; set; }
        public long TimeStamp { get; set; }
    }
}
