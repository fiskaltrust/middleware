using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.FR
{
    public class EfJournalFRRepository : AbstractEFRepostiory<Guid, ftJournalFR>, IJournalFRRepository, IMiddlewareJournalFRRepository
    {
        private long _lastInsertedTimeStamp;

        public EfJournalFRRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftJournalFR> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalFRList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalFRList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<ftJournalFR> GetWithLastTimestampAsync() => Task.FromResult(DbContext.Set<ftJournalFR>().OrderByDescending(x => x.TimeStamp).FirstOrDefault());
        public IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsAsync() => DbContext.JournalFRList.Where(x => x.ReceiptType == "C").ToAsyncEnumerable();
    }
}
