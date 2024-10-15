using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public class InMemoryReceiptJournalRepository : AbstractInMemoryRepository<Guid, ftReceiptJournal>, IReceiptJournalRepository, IMiddlewareReceiptJournalRepository
    {
        public InMemoryReceiptJournalRepository() : base(new List<ftReceiptJournal>()) { }

        public InMemoryReceiptJournalRepository(IEnumerable<ftReceiptJournal> data) : base(data) { }

        protected override void EntityUpdated(ftReceiptJournal entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftReceiptJournal entity) => entity.ftReceiptJournalId;

        public override IAsyncEnumerable<ftReceiptJournal> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftReceiptJournal> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId) => Task.FromResult(Data.Values.FirstOrDefault(x => x.ftQueueItemId == ftQueueItemId));
        
        public Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber) => Task.FromResult(Data.Values.FirstOrDefault(x => x.ftReceiptNumber == ftReceiptNumber));
        
        public Task<ftReceiptJournal> GetWithLastTimestampAsync() => Task.FromResult(Data.Values.OrderByDescending(x => x.TimeStamp).FirstOrDefault());

        public Task<int> CountAsync() => Task.FromResult(Data.Count());
    }
}