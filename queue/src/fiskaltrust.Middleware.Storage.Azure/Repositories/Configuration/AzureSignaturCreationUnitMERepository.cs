using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureSignaturCreationUnitMERepository : BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitME, ftSignaturCreationUnitME>
    {
        public AzureSignaturCreationUnitMERepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftSignaturCreationUnitME)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;

        protected override AzureFtSignaturCreationUnitME MapToAzureEntity(ftSignaturCreationUnitME entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitME MapToStorageEntity(AzureFtSignaturCreationUnitME entity) => Mapper.Map(entity);
    }
}
