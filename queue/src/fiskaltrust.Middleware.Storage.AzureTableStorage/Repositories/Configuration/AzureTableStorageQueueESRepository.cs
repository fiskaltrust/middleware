using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueESRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueES, ftQueueES>
    {
        public AzureTableStorageQueueESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueES)) { }

        protected override void EntityUpdated(ftQueueES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueES entity) => entity.ftQueueESId;

        protected override AzureTableStorageFtQueueES MapToAzureEntity(ftQueueES entity) => Mapper.Map(entity);

        protected override ftQueueES MapToStorageEntity(AzureTableStorageFtQueueES entity) => Mapper.Map(entity);
    }
}

