using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureQueueFRRepository : BaseAzureTableRepository<Guid, AzureFtQueueFR, ftQueueFR>
    {
        public AzureQueueFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueFR)) { }

        protected override void EntityUpdated(ftQueueFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueFR entity) => entity.ftQueueFRId;

        protected override AzureFtQueueFR MapToAzureEntity(ftQueueFR entity) => Mapper.Map(entity);

        protected override ftQueueFR MapToStorageEntity(AzureFtQueueFR entity) => Mapper.Map(entity);
    }
}
