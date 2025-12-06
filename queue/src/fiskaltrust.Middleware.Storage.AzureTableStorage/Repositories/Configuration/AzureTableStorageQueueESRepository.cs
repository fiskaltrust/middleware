using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueESRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueES, ftQueueES>
    {
        public AzureTableStorageQueueESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueES";

        protected override void EntityUpdated(ftQueueES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueES entity) => entity.ftQueueESId;

        public async Task InsertOrUpdateAsync(ftQueueES storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueES MapToAzureEntity(ftQueueES src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueES
            {
                PartitionKey = src.ftQueueESId.ToString(),
                RowKey = src.ftQueueESId.ToString(),
                ftQueueESId = src.ftQueueESId,
                ftSignaturCreationUnitESId = src.ftSignaturCreationUnitESId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDSignQueueItemId = src.SSCDSignQueueItemId,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment?.ToUniversalTime(),
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                CurrentFullInvoiceSeriesNumber = src.CurrentFullInvoiceSeriesNumber,
                CurrentSimplifiedInvoiceSeriesNumber = src.CurrentSimplifiedInvoiceSeriesNumber,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin?.ToUniversalTime(),
                UsedFailedMomentMax = src.UsedFailedMomentMax?.ToUniversalTime(),
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
            };
        }

        protected override ftQueueES MapToStorageEntity(AzureTableStorageFtQueueES src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueES
            {
                ftQueueESId = src.ftQueueESId,
                ftSignaturCreationUnitESId = src.ftSignaturCreationUnitESId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDSignQueueItemId = src.SSCDSignQueueItemId,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                CurrentFullInvoiceSeriesNumber = src.CurrentFullInvoiceSeriesNumber,
                CurrentSimplifiedInvoiceSeriesNumber = src.CurrentSimplifiedInvoiceSeriesNumber,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
            };
        }
    }
}

