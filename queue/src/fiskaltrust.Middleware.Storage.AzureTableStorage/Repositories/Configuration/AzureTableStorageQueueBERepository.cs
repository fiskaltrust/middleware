using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueBERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueBE, ftQueueBE>
    {
        public AzureTableStorageQueueBERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueBE";

        protected override void EntityUpdated(ftQueueBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueBE entity) => entity.ftQueueBEId;

        public async Task InsertOrUpdateAsync(ftQueueBE storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueBE MapToAzureEntity(ftQueueBE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueBE
            {
                PartitionKey = src.ftQueueBEId.ToString(),
                RowKey = src.ftQueueBEId.ToString(),
                ftQueueBEId = src.ftQueueBEId,
                ftSignaturCreationUnitBEId = src.ftSignaturCreationUnitBEId,
                LastHash = src.LastHash,
                LastSignature = src.LastSignature,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment?.ToUniversalTime(),
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin?.ToUniversalTime(),
                UsedFailedMomentMax = src.UsedFailedMomentMax?.ToUniversalTime(),
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
            };
        }

        protected override ftQueueBE MapToStorageEntity(AzureTableStorageFtQueueBE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueBE
            {
                ftQueueBEId = src.ftQueueBEId,
                ftSignaturCreationUnitBEId = src.ftSignaturCreationUnitBEId,
                LastHash = src.LastHash,
                LastSignature = src.LastSignature,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
            };
        }
    }
}
