using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureSignaturCreationUnitFRRepository : BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitFR, ftSignaturCreationUnitFR>
    {
        public AzureSignaturCreationUnitFRRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftSignaturCreationUnitFR)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;

        protected override AzureFtSignaturCreationUnitFR MapToAzureEntity(ftSignaturCreationUnitFR entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitFR MapToStorageEntity(AzureFtSignaturCreationUnitFR entity) => Mapper.Map(entity);
    }
}
