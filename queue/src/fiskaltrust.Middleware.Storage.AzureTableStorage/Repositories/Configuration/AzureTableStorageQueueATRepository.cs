using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueATRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueAT, ftQueueAT>
    {
        public AzureTableStorageQueueATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueAT)) { }

        protected override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;

        protected override AzureTableStorageFtQueueAT MapToAzureEntity(ftQueueAT entity) => Mapper.Map(entity);

        protected override ftQueueAT MapToStorageEntity(AzureTableStorageFtQueueAT entity) => Mapper.Map(entity);
    }
}

