using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueGRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueGR, ftQueueGR>
    {
        public AzureTableStorageQueueGRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueGR";

        protected override void EntityUpdated(ftQueueGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueGR entity) => entity.ftQueueGRId;

        public async Task InsertOrUpdateAsync(ftQueueGR storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueGR MapToAzureEntity(ftQueueGR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueGR
            {
                PartitionKey = src.ftQueueGRId.ToString(),
                RowKey = src.ftQueueGRId.ToString(),
                ftQueueGRId = src.ftQueueGRId,
                ftSignaturCreationUnitGRId = src.ftSignaturCreationUnitGRId,
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

        protected override ftQueueGR MapToStorageEntity(AzureTableStorageFtQueueGR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueGR
            {
                ftQueueGRId = src.ftQueueGRId,
                ftSignaturCreationUnitGRId = src.ftSignaturCreationUnitGRId,
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
