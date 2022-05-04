using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.ME
{
    public class EfJournalMERepository : AbstractEFRepostiory<Guid, ftJournalME>, IJournalMERepository
    {
        private long _lastInsertedTimeStamp;

        public EfJournalMERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftJournalME entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }
        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;
        public override IAsyncEnumerable<ftJournalME> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalMEList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();
        public override IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalMEList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }
        public Task<ftJournalME> GetLastEntryAsync() => Task.FromResult(DbContext.JournalMEList.AsQueryable().OrderByDescending(x => x.TimeStamp).Take(1).FirstOrDefault());
    }
}
