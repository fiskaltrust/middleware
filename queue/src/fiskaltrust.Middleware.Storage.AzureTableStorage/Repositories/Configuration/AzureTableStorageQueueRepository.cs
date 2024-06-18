using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueue, ftQueue>
    {
        public AzureTableStorageQueueRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "Queue";

        protected override void EntityUpdated(ftQueue entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueue entity) => entity.ftQueueId;

        protected override AzureTableStorageFtQueue MapToAzureEntity(ftQueue entity) => Mapper.Map(entity);

        protected override ftQueue MapToStorageEntity(AzureTableStorageFtQueue entity) => Mapper.Map(entity);
    }
}

