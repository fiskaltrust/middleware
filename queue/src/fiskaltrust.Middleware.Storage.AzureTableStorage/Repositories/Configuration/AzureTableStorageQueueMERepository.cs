using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueME, ftQueueME>
    {
        public AzureTableStorageQueueMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueME";

        protected override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;

        protected override AzureTableStorageFtQueueME MapToAzureEntity(ftQueueME src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueME
            {
                PartitionKey = src.ftQueueMEId.ToString(),
                RowKey = src.ftQueueMEId.ToString(),
                ftQueueMEId = src.ftQueueMEId,
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }

        protected override ftQueueME MapToStorageEntity(AzureTableStorageFtQueueME src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueME
            {
                ftQueueMEId = src.ftQueueMEId,
                ftSignaturCreationUnitMEId = src.ftSignaturCreationUnitMEId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }
    }
}

