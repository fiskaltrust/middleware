using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitME, ftSignaturCreationUnitME>
    {
        public AzureTableStorageSignaturCreationUnitMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftSignaturCreationUnitME)) { }

        public const string TABLE_NAME = "SignaturCreationUnitME";

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;

        protected override AzureTableStorageFtSignaturCreationUnitME MapToAzureEntity(ftSignaturCreationUnitME entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitME MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitME entity) => Mapper.Map(entity);
    }
}

