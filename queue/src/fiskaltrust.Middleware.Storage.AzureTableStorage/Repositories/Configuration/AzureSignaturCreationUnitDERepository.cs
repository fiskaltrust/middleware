using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureSignaturCreationUnitDERepository : BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitDE, ftSignaturCreationUnitDE>
    {
        public AzureSignaturCreationUnitDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitDE)) { }

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;

        protected override AzureFtSignaturCreationUnitDE MapToAzureEntity(ftSignaturCreationUnitDE entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitDE MapToStorageEntity(AzureFtSignaturCreationUnitDE entity) => Mapper.Map(entity);
    }
}
