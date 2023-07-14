using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.IT
{
    public class EfJournalITRepository : AbstractEFRepostiory<Guid, ftJournalIT>, IMiddlewareJournalITRepository
    {
        private long _lastInsertedTimeStamp;

        public EfJournalITRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftJournalIT entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }
        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;
        public override IAsyncEnumerable<ftJournalIT> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalITList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();
        public override IAsyncEnumerable<ftJournalIT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalITList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<ftJournalIT> GetByQueueItemId(Guid queueItemId) => Task.FromResult(DbContext.JournalITList.Where(x => x.ftQueueItemId == queueItemId).FirstOrDefault());
    }
}
