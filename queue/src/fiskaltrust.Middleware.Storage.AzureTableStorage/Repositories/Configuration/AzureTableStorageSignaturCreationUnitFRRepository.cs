using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitFRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitFR, ftSignaturCreationUnitFR>
    {
        public AzureTableStorageSignaturCreationUnitFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitFR)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;

        protected override AzureTableStorageFtSignaturCreationUnitFR MapToAzureEntity(ftSignaturCreationUnitFR entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitFR MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitFR entity) => Mapper.Map(entity);
    }
}
