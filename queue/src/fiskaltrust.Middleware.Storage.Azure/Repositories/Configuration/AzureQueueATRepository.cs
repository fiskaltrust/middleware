using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureQueueATRepository : BaseAzureTableRepository<Guid, AzureFtQueueAT, ftQueueAT>
    {
        public AzureQueueATRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftQueueAT)) { }

        protected override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;

        protected override AzureFtQueueAT MapToAzureEntity(ftQueueAT entity) => Mapper.Map(entity);

        protected override ftQueueAT MapToStorageEntity(AzureFtQueueAT entity) => Mapper.Map(entity);
    }
}
