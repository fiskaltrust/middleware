using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.FR
{
    public class InMemoryJournalFRCopyPayloadRepository : AbstractInMemoryRepository<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public InMemoryJournalFRCopyPayloadRepository() : base(new List<ftJournalFRCopyPayload>()) { }

        public InMemoryJournalFRCopyPayloadRepository(IEnumerable<ftJournalFRCopyPayload> data) : base(data) { }

        protected override void EntityUpdated(ftJournalFRCopyPayload entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;

        public override IAsyncEnumerable<ftJournalFRCopyPayload> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalFRCopyPayload> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference) =>
            Task.FromResult(Data.Count(c => c.Value.CopiedReceiptReference == cbPreviousReceiptReference));

        public new async Task<bool> InsertAsync(ftJournalFRCopyPayload c)
        {
            await base.InsertAsync(c);
            return true;
        }
    }
}
