using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitESRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitES, ftSignaturCreationUnitES>
    {
        public AzureTableStorageSignaturCreationUnitESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitES";

        protected override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitES storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtSignaturCreationUnitES MapToAzureEntity(ftSignaturCreationUnitES src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitES
            {
                PartitionKey = src.ftSignaturCreationUnitESId.ToString(),
                RowKey = src.ftSignaturCreationUnitESId.ToString(),
                ftSignaturCreationUnitESId = src.ftSignaturCreationUnitESId,
                Url = src.Url,
                TimeStamp = src.TimeStamp,
                InfoJson = src.InfoJson
            };
        }

        protected override ftSignaturCreationUnitES MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitES src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitES
            {
                ftSignaturCreationUnitESId = src.ftSignaturCreationUnitESId,
                TimeStamp = src.TimeStamp,
                Url = src.Url,
                InfoJson = src.InfoJson
            };
        }
    }
}
