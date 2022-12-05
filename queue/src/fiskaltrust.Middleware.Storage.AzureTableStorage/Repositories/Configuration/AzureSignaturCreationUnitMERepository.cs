using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureSignaturCreationUnitMERepository : BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitME, ftSignaturCreationUnitME>
    {
        public AzureSignaturCreationUnitMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitME)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;

        protected override AzureFtSignaturCreationUnitME MapToAzureEntity(ftSignaturCreationUnitME entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitME MapToStorageEntity(AzureFtSignaturCreationUnitME entity) => Mapper.Map(entity);
    }
}
