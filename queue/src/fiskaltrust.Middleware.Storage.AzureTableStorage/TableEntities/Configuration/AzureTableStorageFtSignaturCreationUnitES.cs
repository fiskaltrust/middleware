using System;
using fiskaltrust.Middleware.Storage.ES;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureTableStorageFtSignaturCreationUnitES : BaseTableEntity
    {
        public Guid ftSignaturCreationUnitESId { get; set; }

        public string StateData { get; set; }

        public long TimeStamp { get; set; }
    }
}
