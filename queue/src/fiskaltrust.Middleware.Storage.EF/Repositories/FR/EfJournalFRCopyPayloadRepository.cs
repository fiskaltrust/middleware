using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.FR
{
    public class EfJournalFRCopyPayloadRepository : AbstractEFRepostiory<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        private long _lastInsertedTimeStamp;

        public EfJournalFRCopyPayloadRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftJournalFRCopyPayload entity)
        {
            if (_lastInsertedTimeStamp == DateTime.UtcNow.Ticks)
            {
                Task.Run(() => Task.Delay(1)).Wait();
            }
            entity.TimeStamp = DateTime.UtcNow.Ticks;
            _lastInsertedTimeStamp = entity.TimeStamp;
        }

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;

        public override IAsyncEnumerable<ftJournalFRCopyPayload> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.JournalFRCopyPayloadList.Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalFRCopyPayload> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = DbContext.JournalFRCopyPayloadList.Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            return await DbContext.JournalFRCopyPayloadList.CountAsync(x => x.CopiedReceiptReference == cbPreviousReceiptReference);
        }
    }
}