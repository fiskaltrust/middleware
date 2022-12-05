using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureQueueDERepository : BaseAzureTableRepository<Guid, AzureFtQueueDE, ftQueueDE>
    {
        public AzureQueueDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueDE)) { }

        protected override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;

        protected override AzureFtQueueDE MapToAzureEntity(ftQueueDE entity) => Mapper.Map(entity);

        protected override ftQueueDE MapToStorageEntity(AzureFtQueueDE entity) => Mapper.Map(entity);
    }
}
