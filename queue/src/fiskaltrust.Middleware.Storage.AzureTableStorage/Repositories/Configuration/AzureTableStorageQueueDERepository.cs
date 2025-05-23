﻿using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueDERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueueDE, ftQueueDE>
    {
        public AzureTableStorageQueueDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "QueueDE";

        protected override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;

        public async Task InsertOrUpdateAsync(ftQueueDE storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtQueueDE MapToAzureEntity(ftQueueDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueueDE
            {
                PartitionKey = src.ftQueueDEId.ToString(),
                RowKey = src.ftQueueDEId.ToString(),
                ftQueueDEId = src.ftQueueDEId,
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
                SSCDFailCount = src.SSCDFailCount,
                SSCDFailMoment = src.SSCDFailMoment?.ToUniversalTime(),
                SSCDFailQueueItemId = src.SSCDFailQueueItemId,
                UsedFailedCount = src.UsedFailedCount,
                UsedFailedMomentMin = src.UsedFailedMomentMin?.ToUniversalTime(),
                UsedFailedMomentMax = src.UsedFailedMomentMax?.ToUniversalTime(),
                UsedFailedQueueItemId = src.UsedFailedQueueItemId,
                TimeStamp = src.TimeStamp,
                DailyClosingNumber = src.DailyClosingNumber
            };
        }

        protected override ftQueueDE MapToStorageEntity(AzureTableStorageFtQueueDE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueueDE
            {
                ftQueueDEId = src.ftQueueDEId,
                ftSignaturCreationUnitDEId = src.ftSignaturCreationUnitDEId,
                LastHash = src.LastHash,
                CashBoxIdentification = src.CashBoxIdentification,
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

