using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureQueueMERepository : BaseAzureTableRepository<Guid, AzureFtQueueME, ftQueueME>
    {
        public AzureQueueMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftQueueME)) { }

        protected override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;

        protected override AzureFtQueueME MapToAzureEntity(ftQueueME entity) => Mapper.Map(entity);

        protected override ftQueueME MapToStorageEntity(AzureFtQueueME entity) => Mapper.Map(entity);
    }
}
