using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ME
{
    public class AzureTableStorageJournalMERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalME, ftJournalME>, IMiddlewareRepository<ftJournalME>, IMiddlewareJournalMERepository
    {
        public AzureTableStorageJournalMERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalME";

        protected override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        protected override AzureTableStorageFtJournalME MapToAzureEntity(ftJournalME src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalME
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftJournalMEId.ToString(),
                ftJournalMEId = src.ftJournalMEId,
                ftQueueItemId = src.ftQueueItemId,
                cbReference = src.cbReference,
                InvoiceNumber = src.InvoiceNumber,
                YearlyOrdinalNumber = src.YearlyOrdinalNumber,
                ftQueueId = src.ftQueueId,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftJournalME MapToStorageEntity(AzureTableStorageFtJournalME src)
        {
            if (src == null)

            {
                return null;
            }

            return new ftJournalME
            {
                ftJournalMEId = src.ftJournalMEId,
                ftQueueItemId = src.ftQueueItemId,
                cbReference = src.cbReference,
                InvoiceNumber = src.InvoiceNumber,
                YearlyOrdinalNumber = src.YearlyOrdinalNumber,
                ftQueueId = src.ftQueueId,
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftJournalME> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(filter: TableClient.CreateQueryFilter<AzureTableStorageFtJournalME>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(filter: TableClient.CreateQueryFilter<AzureTableStorageFtJournalME>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
            return take.HasValue ? result.Take(take.Value) : result;
        }
        public async Task<ftJournalME> GetLastEntryAsync()
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.JournalType == (long) JournalTypes.JournalME);
            return await result.OrderByDescending(x => x.Number).Take(1).Select(MapToStorageEntity).FirstOrDefaultAsync();
        }

        public IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.ftQueueItemId == queueItemId);
            return result.Select(MapToStorageEntity);
        }

        public IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalME>(x => x.cbReference == cbReceiptReference);
            return result.Select(MapToStorageEntity);
        }
    }
}
