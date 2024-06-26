using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR
{
    public class AzureTableStorageJournalFRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalFR, ftJournalFR>, IJournalFRRepository, IMiddlewareRepository<ftJournalFR>
    {
        public AzureTableStorageJournalFRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalFR";

        protected override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        protected override AzureTableStorageFtJournalFR MapToAzureEntity(ftJournalFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalFR
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftJournalFRId.ToString(),
                ftJournalFRId = src.ftJournalFRId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Number = src.Number,
                JWT = src.JWT,
                JsonData = src.JsonData,
                ReceiptType = src.ReceiptType,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftJournalFR MapToStorageEntity(AzureTableStorageFtJournalFR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalFR
            {
                ftJournalFRId = src.ftJournalFRId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Number = src.Number,
                JWT = src.JWT,
                JsonData = src.JsonData,
                ReceiptType = src.ReceiptType,
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}

