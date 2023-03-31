using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.IT
{
    public class InMemoryJournalITRepository : AbstractInMemoryRepository<Guid, ftJournalIT>, IJournalITRepository
    {
        public InMemoryJournalITRepository() : base(new List<ftJournalIT>()) { }

        public InMemoryJournalITRepository(IEnumerable<ftJournalIT> data) : base(data) { }

        protected override void EntityUpdated(ftJournalIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalIT entity) => entity.ftJournalITId;

        public override IAsyncEnumerable<ftJournalIT> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalIT> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
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
