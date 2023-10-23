using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE
{
    public class EfJournalDERepository : AbstractEFRepostiory<Guid, ftJournalDE>, IJournalDERepository, IMiddlewareJournalDERepository
    {
        private long _lastInsertedTimeStamp;

        public EfJournalDERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftJournalDE> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalDEList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalDE> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalDEList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName)
        {
            var result = DbContext.JournalDEList.Where(x => x.FileName == fileName).OrderBy(x => x.TimeStamp);
            return result.ToAsyncEnumerable();
        }
    }
}
