using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitATRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitAT, ftSignaturCreationUnitAT>
    {
        public AzureTableStorageSignaturCreationUnitATRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitAT";

        protected override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity) => entity.ftSignaturCreationUnitATId;

        protected override AzureTableStorageFtSignaturCreationUnitAT MapToAzureEntity(ftSignaturCreationUnitAT entity) => Mapper.Map(entity);

        protected override ftSignaturCreationUnitAT MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitAT entity) => Mapper.Map(entity);
    }
}

