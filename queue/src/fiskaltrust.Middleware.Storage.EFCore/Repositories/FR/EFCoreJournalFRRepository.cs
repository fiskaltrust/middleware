using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.FR
{
    public class EFCoreJournalFRRepository : AbstractEFCoreRepostiory<Guid, ftJournalFR>, IJournalFRRepository, IMiddlewareJournalFRRepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreJournalFRRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftJournalFR entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        public override IAsyncEnumerable<ftJournalFR> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalFRList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalFRList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public async Task<ftJournalFR> GetWithLastTimestampAsync() => await DbContext.JournalFRList.AsQueryable().OrderByDescending(x => x.TimeStamp).FirstOrDefaultAsync().ConfigureAwait(false);

        public IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsAsync() => DbContext.JournalFRList.AsQueryable().Where(x => x.ReceiptType == "C").ToAsyncEnumerable();
    }
}
