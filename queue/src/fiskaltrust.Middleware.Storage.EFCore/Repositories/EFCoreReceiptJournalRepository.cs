using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories
{
    public class EFCoreReceiptJournalRepository : AbstractEFCoreRepostiory<Guid, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareReceiptJournalRepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreReceiptJournalRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftReceiptJournal entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        public override IAsyncEnumerable<ftReceiptJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.ReceiptJournalList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.ReceiptJournalList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public async Task<ftReceiptJournal> GetWithLastTimestampAsync() => await DbContext.ReceiptJournalList.AsQueryable().OrderByDescending(x => x.TimeStamp).FirstOrDefaultAsync().ConfigureAwait(false);
        public async Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId) => await DbContext.ReceiptJournalList.AsQueryable().FirstOrDefaultAsync(x => x.ftQueueItemId == ftQueueItemId).ConfigureAwait(false);
        public async Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber) => await DbContext.ReceiptJournalList.AsQueryable().FirstOrDefaultAsync(x => x.ftReceiptNumber == ftReceiptNumber).ConfigureAwait(false);
        public async Task<int> CountAsync() => await EntityFrameworkQueryableExtensions.CountAsync(DbContext.ReceiptJournalList);
    }
}
