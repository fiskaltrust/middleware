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
    public class AzureTableStorageActionJournalRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtActionJournal, ftActionJournal>, IActionJournalRepository, IMiddlewareRepository<ftActionJournal>, IMiddlewareActionJournalRepository
    {
        public AzureTableStorageActionJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "ActionJournal";

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        protected override AzureTableStorageFtActionJournal MapToAzureEntity(ftActionJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtActionJournal
            {
                PartitionKey = Mapper.GetHashString(src.TimeStamp),
                RowKey = src.ftActionJournalId.ToString(),
                ftActionJournalId = src.ftActionJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Moment = src.Moment.ToUniversalTime(),
                Priority = src.Priority,
                Type = src.Type,
                Message = src.Message,
                DataBase64 = src.DataBase64,
                DataJson = src.DataJson,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftActionJournal MapToStorageEntity(AzureTableStorageFtActionJournal src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftActionJournal
            {
                ftActionJournalId = src.ftActionJournalId,
                ftQueueId = src.ftQueueId,
                ftQueueItemId = src.ftQueueItemId,
                Moment = src.Moment,
                Priority = src.Priority,
                Type = src.Type,
                Message = src.Message,
                DataBase64 = src.DataBase64,
                DataJson = src.DataJson,
                TimeStamp = src.TimeStamp
            };
        }

        public IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value) : result;
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtActionJournal>(x => x.ftQueueItemId == queueItemId);
            return result.Select(MapToStorageEntity);
        }

        public async Task<ftActionJournal> GetWithLastTimestampAsync()
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtActionJournal>(filter: "", maxPerPage: 1);
            return MapToStorageEntity(await result.FirstOrDefaultAsync());
        }

        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive)
        {
            var result = _tableClient.QueryAsync<AzureTableStorageFtActionJournal>(filter: TableClient.CreateQueryFilter($"PartitionKey le {Mapper.GetHashString(fromTimestampInclusive)} and Priority lt {lowerThanPriority}"));
            return result.Select(MapToStorageEntity);
        }
    }
}