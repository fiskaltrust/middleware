using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.ME
{
    public class AzureJournalMERepository : BaseAzureTableRepository<Guid, AzureFtJournalME, ftJournalME>, IMiddlewareRepository<ftJournalME>, IMiddlewareJournalMERepository
    {
        public AzureJournalMERepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftJournalME)) { }

        protected override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        protected override AzureFtJournalME MapToAzureEntity(ftJournalME entity) => Mapper.Map(entity);

        protected override ftJournalME MapToStorageEntity(AzureFtJournalME entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var tableQuery = new TableQuery<AzureFtJournalME>();
            tableQuery = tableQuery.Where(TableQuery.GenerateFilterConditionForLong("TimeStamp", QueryComparisons.GreaterThanOrEqual, fromInclusive));
            var result = GetAllByTableFilterAsync(tableQuery).ToListAsync().Result;
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }
        public Task<ftJournalME> GetLastEntryAsync()
        {
            var tableQuery = new TableQuery<AzureFtJournalME>();
            tableQuery = tableQuery.Where(TableQuery.GenerateFilterConditionForLong("JournalType", QueryComparisons.Equal, (long) JournalTypes.JournalME)).OrderByDesc("Number");
            return Task.FromResult(GetAllByTableFilterAsync(tableQuery).Take(1).ToListAsync().Result.FirstOrDefault());
        }

        public async IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var filter = TableQuery.GenerateFilterCondition(nameof(ftJournalME.ftQueueItemId), QueryComparisons.Equal, queueItemId.ToString());
            var result = await GetAllAsync(filter).ToListAsync();
            foreach (var item in result)
            {
                yield return MapToStorageEntity(item);
            }
        }

        public async IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var filter = TableQuery.GenerateFilterCondition(nameof(ftJournalME.cbReference), QueryComparisons.Equal, cbReceiptReference);
            var result = await GetAllAsync(filter).ToListAsync();
            foreach (var item in result)
            {
                yield return MapToStorageEntity(item);
            }
        }
    }
}
