using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.AT
{
    public class InMemoryJournalATRepository : AbstractInMemoryRepository<Guid, ftJournalAT>, IJournalATRepository
    {
        public InMemoryJournalATRepository() : base(new List<ftJournalAT>()) { }

        public InMemoryJournalATRepository(IEnumerable<ftJournalAT> data) : base(data) { }

        protected override void EntityUpdated(ftJournalAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalAT entity) => entity.ftJournalATId;

        public override IAsyncEnumerable<ftJournalAT> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalAT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
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
