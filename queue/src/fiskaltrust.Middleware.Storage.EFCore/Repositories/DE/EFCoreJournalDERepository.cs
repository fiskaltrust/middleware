using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE
{
    public class EFCoreJournalDERepository : AbstractEFCoreRepostiory<Guid, ftJournalDE>, IJournalDERepository, IMiddlewareJournalDERepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreJournalDERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftJournalDE entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftJournalDE entity) => entity.ftJournalDEId;

        public override IAsyncEnumerable<ftJournalDE> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalDEList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalDEList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var result = DbContext.JournalDEList.AsQueryable().Where(x => x.FileName == fileName).OrderBy(x => x.TimeStamp);
            return result.AsAsyncEnumerable();
        }
    }
}
