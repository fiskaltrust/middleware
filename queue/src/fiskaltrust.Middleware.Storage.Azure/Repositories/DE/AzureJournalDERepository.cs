using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.DE
{
    public class AzureJournalDERepository : BaseAzureTableRepository<Guid, AzureFtJournalDE, ftJournalDE>, IJournalDERepository, IMiddlewareRepository<ftJournalDE>, IMiddlewareJournalDERepository
    {
        public AzureJournalDERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftJournalDE)) { }

        protected override void EntityUpdated(ftJournalDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;

        protected override AzureFtJournalDE MapToAzureEntity(ftJournalDE entity) => Mapper.Map(entity);

        protected override ftJournalDE MapToStorageEntity(AzureFtJournalDE entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = base.GetEntriesOnOrAfterTimeStampAsync(fromInclusive).OrderBy(x => x.TimeStamp);
            return take.HasValue ? result.Take(take.Value).AsAsyncEnumerable() : result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var result = _tableClient.QueryAsync<AzureFtJournalDE>(filter: TableClient.CreateQueryFilter($"FileName eq {fileName}"));
            return result.Select(MapToStorageEntity).AsAsyncEnumerable();
        }
    }
}
