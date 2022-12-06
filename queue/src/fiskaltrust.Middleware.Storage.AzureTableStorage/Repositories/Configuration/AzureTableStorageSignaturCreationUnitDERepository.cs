using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitDERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitDE, ftSignaturCreationUnitDE>
    {
        public AzureTableStorageSignaturCreationUnitDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitDE)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;

        protected override AzureTableStorageFtSignaturCreationUnitDE MapToAzureEntity(ftSignaturCreationUnitDE entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitDE MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitDE entity) => Mapper.Map(entity);
    }
}
