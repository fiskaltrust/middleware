using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.Middleware.Storage.Repositories;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ES
{
    public class AzureTableStorageJournalESRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalES, ftJournalES>, IJournalESRepository, IMiddlewareRepository<ftJournalES>
    {
        public AzureTableStorageJournalESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalES";

        protected override void EntityUpdated(ftJournalES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalES entity) => entity.ftJournalESId;

        protected override AzureTableStorageFtJournalES MapToAzureEntity(ftJournalES src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalES
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftJournalESId.ToString(),
                ftJournalESId = src.ftJournalESId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                JournalType = src.JournalType,
                JournalData = src.JournalData,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftJournalES MapToStorageEntity(AzureTableStorageFtJournalES src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalES
            {
                ftJournalESId = src.ftJournalESId,
                ftQueueId = src.ftQueueId,
                ftSignaturCreationUnitId = src.ftSignaturCreationUnitId,
                JournalType = src.JournalType,
                JournalData = src.JournalData,
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftJournalES> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalES>(filter:
                TableClient.CreateQueryFilter<AzureTableStorageFtJournalES>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        private IAsyncEnumerable<ftJournalES> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalES>(filter:
                TableClient.CreateQueryFilter<AzureTableStorageFtJournalES>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0));

            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalES> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}