using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueITRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueIT, ftQueueIT>
    {
        public AzureTableStorageQueueITRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueIT";

        protected override void EntityUpdated(ftQueueIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueIT entity) => entity.ftQueueITId;

        protected override AzureTableStorageFtQueueIT MapToAzureEntity(ftQueueIT src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueIT
            {
                PartitionKey = src.ftQueueITId.ToString(),
                RowKey = src.ftQueueITId.ToString(),
                ftQueueITId = src.ftQueueITId,
                ftSignaturCreationUnitITId = src.ftSignaturCreationUnitITId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                CashBoxIdentification = src.CashBoxIdentification,
            };
        }

        protected override ftQueueIT MapToStorageEntity(AzureTableStorageFtQueueIT src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueIT
            {
                ftQueueITId = src.ftQueueITId,
                ftSignaturCreationUnitITId = src.ftSignaturCreationUnitITId,
                LastHash = src.LastHash,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment,
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin,
                UsedFailedMomentMax = src.UsedFailedMomentMax,
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                CashBoxIdentification = src.CashBoxIdentification
            };
        }
    }
}

