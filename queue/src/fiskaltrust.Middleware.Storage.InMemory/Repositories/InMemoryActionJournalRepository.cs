using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public class InMemoryActionJournalRepository : AbstractInMemoryRepository<Guid, ftActionJournal>, IActionJournalRepository
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
    }
}