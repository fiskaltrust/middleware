using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageReceiptJournalRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtReceiptJournal, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>
    {
        public AzureTableStorageReceiptJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "ReceiptJournal";

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        protected override AzureTableStorageFtReceiptJournal MapToAzureEntity(ftReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtReceiptJournal
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftReceiptJournalId.ToString(),
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment.ToUniversalTime(),
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDouble(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftReceiptJournal MapToStorageEntity(AzureTableStorageFtReceiptJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftReceiptJournal
            {
                ftReceiptJournalId = src.ftReceiptJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                ftReceiptHash = src.ftReceiptHash,
                ftReceiptMoment = src.ftReceiptMoment,
                ftReceiptNumber = src.ftReceiptNumber,
                ftReceiptTotal = Convert.ToDecimal(src.ftReceiptTotal),
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftReceiptJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtReceiptJournal>(filter: TableClient.CreateQueryFilter<AzureTableStorageFtReceiptJournal>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtReceiptJournal>(filter: TableClient.CreateQueryFilter<AzureTableStorageFtReceiptJournal>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        public Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId) => throw new NotImplementedException();
        public Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber) => throw new NotImplementedException();
        public async Task<ftReceiptJournal> GetWithLastTimestampAsync()
            => MapToStorageEntity(await _tableClient.QueryAsync<AzureTableStorageFtReceiptJournal>().OrderBy(x => x.TimeStamp).LastAsync());


        public async Task<int> CountAsync()
        {
            var results = _tableClient.QueryAsync<TableEntity>(select: new string[] { });
            return await results.CountAsync().ConfigureAwait(false);
        }
    }
}

