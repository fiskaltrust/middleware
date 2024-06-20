using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitIT, ftSignaturCreationUnitIT>
    {
        public AzureTableStorageSignaturCreationUnitITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitIT";

        protected override void EntityUpdated(ftSignaturCreationUnitIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitIT entity) => entity.ftSignaturCreationUnitITId;

        protected override AzureTableStorageFtSignaturCreationUnitIT MapToAzureEntity(ftSignaturCreationUnitIT entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitIT MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitIT entity) => Mapper.Map(entity);
    }
}
