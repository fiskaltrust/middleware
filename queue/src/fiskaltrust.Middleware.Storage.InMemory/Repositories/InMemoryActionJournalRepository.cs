using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public class InMemoryActionJournalRepository : AbstractInMemoryRepository<Guid, ftActionJournal>, IActionJournalRepository, IMiddlewareActionJournalRepository
    {
        public InMemoryActionJournalRepository() : base(new List<ftActionJournal>()) { }

        public InMemoryActionJournalRepository(IEnumerable<ftActionJournal> data) : base(data) { }

        protected override void EntityUpdated(ftActionJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftActionJournal entity) => entity.ftActionJournalId;

        public override IAsyncEnumerable<ftActionJournal>  GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftActionJournal>  GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null) 
        {
            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId)
        {
            var result = Data.Select(x => x.Value).Where(x => x.ftQueueItemId == queueItemId).OrderByDescending(x => x.TimeStamp);
            return result.ToAsyncEnumerable();
        }

        public Task<ftActionJournal> GetWithLastTimestampAsync() => Task.FromResult(Data.Values.OrderByDescending(x => x.TimeStamp).FirstOrDefault());
        public IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive) =>
            Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromTimestampInclusive && x.Priority < lowerThanPriority).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

    }
}