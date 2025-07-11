using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueEURepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueEU, ftQueueEU>
    {
        public AzureTableStorageQueueEURepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueEU";

        protected override void EntityUpdated(ftQueueEU entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueEU entity) => entity.ftQueueEUId;

        public async Task InsertOrUpdateAsync(ftQueueEU storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueEU MapToAzureEntity(ftQueueEU src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueEU
            {
                PartitionKey = src.ftQueueEUId.ToString(),
                RowKey = src.ftQueueEUId.ToString(),
                ftQueueEUId = src.ftQueueEUId,
                CashBoxIdentification = src.CashBoxIdentification,
                TimeStamp = src.TimeStamp,
            };
        }

        protected override ftQueueEU MapToStorageEntity(AzureTableStorageFtQueueEU src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueEU
            {
                ftQueueEUId = src.ftQueueEUId,
                CashBoxIdentification = src.CashBoxIdentification,
                TimeStamp = src.TimeStamp,
            };
        }
    }
}

