using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitGR : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitGRId { get; set; }
        public long TimeStamp { get; set; }
    }
}
