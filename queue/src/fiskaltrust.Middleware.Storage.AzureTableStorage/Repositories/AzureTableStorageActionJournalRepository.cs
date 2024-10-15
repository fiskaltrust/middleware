using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageActionJournalRepository : BaseAzureTableStorageRepository<Guid, TableEntity, ftActionJournal>, IActionJournalRepository, IMiddlewareRepository<ftActionJournal>, IMiddlewareActionJournalRepository

    {
        public AzureTableStorageActionJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "ActionJournal";

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        protected override TableEntity MapToAzureEntity(ftActionJournal src)
        {
            if (src == null)
            {
                return null;
            }

            var entity = new TableEntity(Mapper.GetHashString(src.TimeStamp), src.ftActionJournalId.ToString())
            {
                {nameof(ftActionJournal.ftActionJournalId), src.ftActionJournalId},
                {nameof(ftActionJournal.ftQueueId), src.ftQueueId},
                {nameof(ftActionJournal.ftQueueItemId), src.ftQueueItemId},
                {nameof(ftActionJournal.Moment), src.Moment.ToUniversalTime()},
                {nameof(ftActionJournal.Priority), src.Priority},
                {nameof(ftActionJournal.Type), src.Type},
                {nameof(ftActionJournal.Message), src.Message},
                {nameof(ftActionJournal.TimeStamp), src.TimeStamp}
            };

            entity.SetOversized(nameof(ftActionJournal.DataBase64), src.DataBase64);
            entity.SetOversized(nameof(ftActionJournal.DataJson), src.DataJson);

            return entity;
        }

        protected override ftActionJournal MapToStorageEntity(TableEntity src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftActionJournal
            {
                ftActionJournalId = src.GetGuid(nameof(ftActionJournal.ftActionJournalId)).GetValueOrDefault(),
                ftQueueId = src.GetGuid(nameof(ftActionJournal.ftQueueId)).GetValueOrDefault(),
                ftQueueItemId = src.GetGuid(nameof(ftActionJournal.ftQueueItemId)).GetValueOrDefault(),
                Moment = src.GetDateTime(nameof(ftActionJournal.Moment)).GetValueOrDefault(),
                Priority = src.GetInt32(nameof(ftActionJournal.Priority)).GetValueOrDefault(),
                Type = src.GetString(nameof(ftActionJournal.Type)),
                Message = src.GetString(nameof(ftActionJournal.Message)),
                DataBase64 = src.GetOversized(nameof(ftActionJournal.DataBase64)),
                DataJson = src.GetOversized(nameof(ftActionJournal.DataJson)),
                TimeStamp = src.GetInt64(nameof(ftActionJournal.TimeStamp)).GetValueOrDefault()
            };
        }

        public IAsyncEnumerable<ftActionJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<TableEntity>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0 && x.PartitionKey.CompareTo(Mapper.GetHashString(toInclusive)) >= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<TableEntity>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromInclusive)) <= 0));
            return result.Select(MapToStorageEntity).OrderBy(x => x.TimeStamp);
        }

        public IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<ftActionJournal>(x => x.ftQueueItemId == queueItemId));
            return result.Select(MapToStorageEntity);
        }

        public async Task<ftActionJournal> GetWithLastTimestampAsync()
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: "", maxPerPage: 1);
            return MapToStorageEntity(await result.FirstOrDefaultAsync());
        }

        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive)
        {
            var result = _tableClient.QueryAsync<TableEntity>(filter: TableClient.CreateQueryFilter<TableEntity>(x => x.PartitionKey.CompareTo(Mapper.GetHashString(fromTimestampInclusive)) <= 0 && x.GetInt32(nameof(ftActionJournal.Priority)) < lowerThanPriority));
            return result.Select(MapToStorageEntity);
        }

        public async Task<int> CountAsync() {
            var results = _tableClient.QueryAsync<TableEntity>(select: new string[] { });
            return await results.CountAsync().ConfigureAwait(false);
        }
    }
}