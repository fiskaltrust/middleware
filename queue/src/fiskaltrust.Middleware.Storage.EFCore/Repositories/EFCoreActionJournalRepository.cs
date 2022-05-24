using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories
{
    public class EFCoreActionJournalRepository : AbstractEFCoreRepostiory<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreActionJournalRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftActionJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.ActionJournalList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftActionJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.ActionJournalList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = DbContext.ActionJournalList.AsQueryable().Where(x => x.ftQueueItemId == queueItemId).OrderBy(x => x.TimeStamp);
            return result.AsAsyncEnumerable();
        }
    }
}
