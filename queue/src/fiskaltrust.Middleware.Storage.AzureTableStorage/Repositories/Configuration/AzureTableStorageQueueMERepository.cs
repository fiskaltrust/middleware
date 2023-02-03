using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueME, ftQueueME>
    {
        public AzureTableStorageQueueMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueME)) { }

        protected override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;

        protected override AzureTableStorageFtQueueME MapToAzureEntity(ftQueueME entity) => Mapper.Map(entity);

        protected override ftQueueME MapToStorageEntity(AzureTableStorageFtQueueME entity) => Mapper.Map(entity);
    }
}

