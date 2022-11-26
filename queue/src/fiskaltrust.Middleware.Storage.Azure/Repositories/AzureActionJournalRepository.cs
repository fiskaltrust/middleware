using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public class AzureActionJournalRepository : BaseAzureTableRepository<Guid, AzureFtActionJournal, ftActionJournal>, IActionJournalRepository, IMiddlewareRepository<ftActionJournal>, IMiddlewareActionJournalRepository
    {
        public AzureActionJournalRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftActionJournal)) { }

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        protected override AzureFtActionJournal MapToAzureEntity(ftActionJournal entity) => Mapper.Map(entity);

        protected override ftActionJournal MapToStorageEntity(AzureFtActionJournal entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value).AsAsyncEnumerable() : result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = _tableClient.QueryAsync<AzureFtActionJournal>(filter: TableClient.CreateQueryFilter($"ftQueueItemId eq {queueItemId}"));
            return result.Select(MapToStorageEntity).AsAsyncEnumerable();
        }
    }
}