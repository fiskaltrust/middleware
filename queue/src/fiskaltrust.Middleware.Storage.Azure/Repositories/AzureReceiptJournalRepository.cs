using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public class AzureReceiptJournalRepository : BaseAzureTableRepository<Guid, AzureFtReceiptJournal, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>, IMiddlewareReceiptJournalRepository
    {
        public AzureReceiptJournalRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftReceiptJournal)) { }

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        protected override AzureFtReceiptJournal MapToAzureEntity(ftReceiptJournal entity) => Mapper.Map(entity);

        protected override ftReceiptJournal MapToStorageEntity(AzureFtReceiptJournal entity) => Mapper.Map(entity);

        public IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = GetEntriesOnOrAfterTimeStampAsync(fromInclusive).ToListAsync().Result.OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public async Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId)
        {
            var query = new TableQuery<AzureFtReceiptJournal>();
            query = query.Where(TableQuery.GenerateFilterCondition(nameof(AzureFtReceiptJournal.ftQueueItemId), QueryComparisons.Equal, ftQueueItemId.ToString()));

            var result = GetAllByTableFilterAsync(query);

            return await result.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber)
        {
            var query = new TableQuery<AzureFtReceiptJournal>();
            query = query.Where(TableQuery.GenerateFilterCondition(nameof(AzureFtReceiptJournal.ftReceiptNumber), QueryComparisons.Equal, ftReceiptNumber.ToString()));

            var result = GetAllByTableFilterAsync(query);

            return await result.FirstOrDefaultAsync().ConfigureAwait(false);
        }
    }
}
