using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ES
{
    public class AzureTableStorageJournalESRepository : BaseAzureTableStorageRepository<Guid, TableEntity, ftJournalES>, IJournalESRepository, IMiddlewareRepository<ftJournalES>
    {
        public AzureTableStorageJournalESRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalES";

        protected override void EntityUpdated(ftJournalES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalES entity) => entity.ftJournalESId;

        protected override TableEntity MapToAzureEntity(ftJournalES src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.TimeStamp), src.ftJournalESId.ToString())
            {
                { nameof(ftJournalES.ftJournalESId), src.ftJournalESId},
                { nameof(ftJournalES.ftQueueId), src.ftQueueId},
                { nameof(ftJournalES.ftQueueItemId), src.ftQueueItemId},
                { nameof(ftJournalES.JournalType), src.JournalType},
                { nameof(ftJournalES.RequestData), src.RequestData},
                { nameof(ftJournalES.ResponseData), src.ResponseData},
                { nameof(ftJournalES.Number), src.Number},
                { nameof(ftJournalES.TimeStamp), src.TimeStamp},
            };

            entity.SetOversized(nameof(ftJournalES.RequestData), src.RequestData);
            entity.SetOversized(nameof(ftJournalES.ResponseData), src.ResponseData);

            return entity;
        }

        protected override ftJournalES MapToStorageEntity(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalES
            {
                ftJournalESId = src.GetGuid(nameof(ftJournalES.ftJournalESId)).GetValueOrDefault(),
                ftQueueId = src.GetGuid(nameof(ftJournalES.ftQueueId)).GetValueOrDefault(),
                ftQueueItemId = src.GetGuid(nameof(ftJournalES.ftQueueItemId)).GetValueOrDefault(),
                JournalType = src.GetString(nameof(ftJournalES.JournalType)),
                RequestData = src.GetOversized(nameof(ftJournalES.RequestData)),
                ResponseData = src.GetOversized(nameof(ftJournalES.ResponseData)),
                TimeStamp = src.GetInt64(nameof(ftQueueItem.TimeStamp)).GetValueOrDefault(),
            };
        }

        public IAsyncEnumerable<ftJournalES> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter:
                TableClient.CreateQueryFilter<TableEntity>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalES> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter:
                TableClient.CreateQueryFilter<TableEntity>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0));

            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftJournalES> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
            return take.HasValue ? result.Take(take.Value) : result;
        }
    }
}