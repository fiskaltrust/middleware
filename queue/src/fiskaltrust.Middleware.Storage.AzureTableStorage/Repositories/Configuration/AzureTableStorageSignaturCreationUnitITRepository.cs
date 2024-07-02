using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitIT, ftSignaturCreationUnitIT>
    {
        public AzureTableStorageSignaturCreationUnitITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitIT";

        protected override void EntityUpdated(ftSignaturCreationUnitIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitIT entity) => entity.ftSignaturCreationUnitITId;

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitIT storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtSignaturCreationUnitIT MapToAzureEntity(ftSignaturCreationUnitIT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitIT
            {
                PartitionKey = src.ftSignaturCreationUnitITId.ToString(),
                RowKey = src.ftSignaturCreationUnitITId.ToString(),
                ftSignaturCreationUnitITId = src.ftSignaturCreationUnitITId,
                Url = src.Url,
                TimeStamp = src.TimeStamp,
                InfoJson = src.InfoJson
            };
        }

        protected override ftSignaturCreationUnitIT MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitIT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitIT
            {
                ftSignaturCreationUnitITId = src.ftSignaturCreationUnitITId,
                TimeStamp = src.TimeStamp,
                Url = src.Url,
                InfoJson = src.InfoJson
            };
        }
    }
}
