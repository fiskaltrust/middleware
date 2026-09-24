using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueuePLRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueuePL, ftQueuePL>
    {
        public AzureTableStorageQueuePLRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueuePL";

        protected override void EntityUpdated(ftQueuePL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueuePL entity) => entity.ftQueuePLId;

        public async Task InsertOrUpdateAsync(ftQueuePL storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueuePL MapToAzureEntity(ftQueuePL src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueuePL
            {
                PartitionKey = src.ftQueuePLId.ToString(),
                RowKey = src.ftQueuePLId.ToString(),
                ftQueuePLId = src.ftQueuePLId,
                ftSignaturCreationUnitPLId = src.ftSignaturCreationUnitPLId,
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

        protected override ftQueuePL MapToStorageEntity(AzureTableStorageFtQueuePL src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueuePL
            {
                ftQueuePLId = src.ftQueuePLId,
                ftSignaturCreationUnitPLId = src.ftSignaturCreationUnitPLId,
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
