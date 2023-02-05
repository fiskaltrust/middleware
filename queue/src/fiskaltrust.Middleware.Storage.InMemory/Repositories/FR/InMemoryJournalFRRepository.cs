using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.FR
{
    public class InMemoryJournalFRRepository : AbstractInMemoryRepository<Guid, ftJournalFR>, IJournalFRRepository, IMiddlewareJournalFRRepository
    {
        public InMemoryJournalFRRepository() : base(new List<ftJournalFR>()) { }

        public InMemoryJournalFRRepository(IEnumerable<ftJournalFR> data) : base(data) { }

        protected override void EntityUpdated(ftJournalFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFR entity) => entity.ftJournalFRId;

        public override IAsyncEnumerable<ftJournalFR> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalFR> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<ftJournalFR> GetWithLastTimestampAsync() => Task.FromResult(Data.Values.OrderByDescending(x => x.TimeStamp).FirstOrDefault());

        public IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsAsync() => Data.Values.Where(x => x.ReceiptType == "C").ToAsyncEnumerable();
    }
}
