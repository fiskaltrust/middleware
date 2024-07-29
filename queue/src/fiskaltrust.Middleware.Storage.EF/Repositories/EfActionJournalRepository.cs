using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories
{
    public class EfActionJournalRepository : AbstractEFRepostiory<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
    {
        private long _lastInsertedTimeStamp;

        public EfActionJournalRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftActionJournal entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        public override IAsyncEnumerable<ftActionJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.ActionJournalList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.ActionJournalList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = DbContext.ActionJournalList.Where(x => x.ftQueueItemId == queueItemId).OrderByDescending(x => x.TimeStamp);
            return result.ToAsyncEnumerable();
        }

        public Task<ftActionJournal> GetWithLastTimestampAsync() => Task.FromResult(DbContext.Set<ftActionJournal>().OrderByDescending(x => x.TimeStamp).FirstOrDefault());
        
        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive) =>
            DbContext.ActionJournalList.Where(x => x.TimeStamp >= fromTimestampInclusive && x.Priority < lowerThanPriority).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public async Task<int> CountAsync() => await DbContext.ActionJournalList.CountAsync();
    }
}
