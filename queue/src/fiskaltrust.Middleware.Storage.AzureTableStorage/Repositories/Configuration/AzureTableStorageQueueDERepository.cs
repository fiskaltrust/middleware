using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueDERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueDE, ftQueueDE>
    {
        public AzureTableStorageQueueDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueDE)) { }

        protected override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;

        protected override AzureTableStorageFtQueueDE MapToAzureEntity(ftQueueDE entity) => Mapper.Map(entity);

        protected override ftQueueDE MapToStorageEntity(AzureTableStorageFtQueueDE entity) => Mapper.Map(entity);
    }
}

