using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureSignaturCreationUnitDERepository : BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitDE, ftSignaturCreationUnitDE>
    {
        public AzureSignaturCreationUnitDERepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftSignaturCreationUnitDE)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;

        protected override AzureFtSignaturCreationUnitDE MapToAzureEntity(ftSignaturCreationUnitDE entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitDE MapToStorageEntity(AzureFtSignaturCreationUnitDE entity) => Mapper.Map(entity);
    }
}
