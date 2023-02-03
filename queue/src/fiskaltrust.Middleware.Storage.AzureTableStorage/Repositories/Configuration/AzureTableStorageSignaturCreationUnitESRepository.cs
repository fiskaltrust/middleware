using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitESRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitES, ftSignaturCreationUnitES>
    {
        public AzureTableStorageSignaturCreationUnitESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitES)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;

        protected override AzureTableStorageFtSignaturCreationUnitES MapToAzureEntity(ftSignaturCreationUnitES entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitES MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitES entity) => Mapper.Map(entity);
    }
}
