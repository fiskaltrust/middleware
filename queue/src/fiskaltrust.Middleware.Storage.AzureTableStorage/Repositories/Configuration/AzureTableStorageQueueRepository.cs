using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageQueueRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtQueue, ftQueue>
    {
        public AzureTableStorageQueueRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "Queue";

        protected override void EntityUpdated(ftQueue entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueue entity) => entity.ftQueueId;

        protected override AzureTableStorageFtQueue MapToAzureEntity(ftQueue src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtQueue
            {
                PartitionKey = src.ftQueueId.ToString(),
                RowKey = src.ftQueueId.ToString(),
                ftQueueId = src.ftQueueId,
                ftCashBoxId = src.ftCashBoxId,
                ftCurrentRow = src.ftCurrentRow,
                ftQueuedRow = src.ftQueuedRow,
                ftReceiptNumerator = src.ftReceiptNumerator,
                ftReceiptTotalizer = Convert.ToDouble(src.ftReceiptTotalizer),
                ftReceiptHash = src.ftReceiptHash,
                StartMoment = src.StartMoment,
                StopMoment = src.StopMoment,
                CountryCode = src.CountryCode,
                Timeout = src.Timeout,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftQueue MapToStorageEntity(AzureTableStorageFtQueue src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftQueue
            {
                ftQueueId = src.ftQueueId,
                ftCashBoxId = src.ftCashBoxId,
                ftCurrentRow = src.ftCurrentRow,
                ftQueuedRow = src.ftQueuedRow,
                ftReceiptNumerator = src.ftReceiptNumerator,
                ftReceiptTotalizer = Convert.ToDecimal(src.ftReceiptTotalizer),
                ftReceiptHash = src.ftReceiptHash,
                StartMoment = src.StartMoment,
                StopMoment = src.StopMoment,
                CountryCode = src.CountryCode,
                Timeout = src.Timeout,
                TimeStamp = src.TimeStamp
            };
        }
    }
}

