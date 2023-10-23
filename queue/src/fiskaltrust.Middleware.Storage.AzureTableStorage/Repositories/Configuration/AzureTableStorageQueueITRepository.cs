using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueIT, ftQueueIT>
    {
        public AzureTableStorageQueueITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueIT)) { }

        protected override void EntityUpdated(ftQueueIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueIT entity) => entity.ftQueueITId;

        protected override AzureTableStorageFtQueueIT MapToAzureEntity(ftQueueIT entity) => Mapper.Map(entity);

        protected override ftQueueIT MapToStorageEntity(AzureTableStorageFtQueueIT entity) => Mapper.Map(entity);
    }
}

