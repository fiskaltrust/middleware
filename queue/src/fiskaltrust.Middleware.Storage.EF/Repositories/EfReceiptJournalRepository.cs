using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories
{
    public class EfReceiptJournalRepository : AbstractEFRepostiory<Guid, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareReceiptJournalRepository
    {
        private long _lastInsertedTimeStamp;

        public EfReceiptJournalRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftReceiptJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.ReceiptJournalList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.ReceiptJournalList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<ftReceiptJournal> GetWithLastTimestampAsync() => Task.FromResult(DbContext.Set<ftReceiptJournal>().OrderByDescending(x => x.TimeStamp).FirstOrDefault());

        public Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId) => Task.FromResult(DbContext.Set<ftReceiptJournal>().FirstOrDefault(x => x.ftQueueItemId == ftQueueItemId));

        public Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber) => Task.FromResult(DbContext.Set<ftReceiptJournal>().FirstOrDefault(x => x.ftReceiptNumber == ftReceiptNumber));

        public async Task<int> CountAsync() => await DbContext.ReceiptJournalList.CountAsync();
    }
}
