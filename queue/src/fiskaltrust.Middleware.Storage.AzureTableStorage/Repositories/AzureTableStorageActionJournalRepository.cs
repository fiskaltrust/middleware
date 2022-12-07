using System;
using System.Collections.Generic;
using System.Linq;
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
            : base(queueConfig, tableServiceClient, nameof(ftActionJournal)) { }

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        protected override AzureTableStorageFtActionJournal MapToAzureEntity(ftActionJournal entity) => Mapper.Map(entity);

        protected override ftActionJournal MapToStorageEntity(AzureTableStorageFtActionJournal entity) => Mapper.Map(entity);

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
    }
}