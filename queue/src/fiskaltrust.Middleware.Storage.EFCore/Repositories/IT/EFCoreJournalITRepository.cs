using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.IT
{
    public class EFCoreJournalITRepository : AbstractEFCoreRepostiory<Guid, ftJournalIT>, IJournalITRepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreJournalITRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftJournalIT> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalITList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalIT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalITList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }
    }
}
