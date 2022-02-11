using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureQueueDERepository : BaseAzureTableRepository<Guid, AzureFtQueueDE, ftQueueDE>
    {
        public AzureQueueDERepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftQueueDE)) { }

        protected override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;

        protected override AzureFtQueueDE MapToAzureEntity(ftQueueDE entity) => Mapper.Map(entity);

        protected override ftQueueDE MapToStorageEntity(AzureFtQueueDE entity) => Mapper.Map(entity);
    }
}
