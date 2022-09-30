using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.EntityFrameworkCore;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.ME
{
    public class EFCoreJournalMERepository : AbstractEFCoreRepostiory<Guid, ftJournalME>, IJournalMERepository, IMiddlewareJournalMERepository
    {
        private long _lastInsertedTimeStamp;

        public EFCoreJournalMERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

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

        public override IAsyncEnumerable<ftJournalME> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalMEList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).AsAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalMEList.AsQueryable().Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).AsAsyncEnumerable();
            }
            return result.AsAsyncEnumerable();
        }

        public Task<ftJournalME> GetLastEntryAsync() => Task.FromResult(DbContext.JournalMEList.AsQueryable().Where(x => x.JournalType == (long) JournalTypes.JournalME).OrderByDescending(x => x.Number).Take(1).FirstOrDefault());
        
        public IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var result = DbContext.JournalMEList.AsQueryable().Where(x => x.ftQueueItemId == queueItemId).OrderBy(x => x.TimeStamp);
            return result.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var result = DbContext.JournalMEList.AsQueryable().Where(x => x.cbReference == cbReceiptReference).OrderBy(x => x.TimeStamp);
            return result.AsAsyncEnumerable();
        }
    }
}
