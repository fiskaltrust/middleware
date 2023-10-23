﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.ME
{
    public class InMemoryJournalMERepository : AbstractInMemoryRepository<Guid, ftJournalME>, IJournalMERepository, IMiddlewareJournalMERepository
    {
        public InMemoryJournalMERepository() : base(new List<ftJournalME>()) { }

        public InMemoryJournalMERepository(IEnumerable<ftJournalME> data) : base(data) { }

        protected override void EntityUpdated(ftJournalME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalME entity) => entity.ftJournalMEId;

        public override IAsyncEnumerable<ftJournalME> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive && x.TimeStamp <= toInclusive).OrderBy(x => x.TimeStamp).ToAsyncEnumerable();

        public override IAsyncEnumerable<ftJournalME> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var result = Data.Select(x => x.Value).Where(x => x.TimeStamp >= fromInclusive).OrderBy(x => x.TimeStamp);
            if (take.HasValue)
            {
                return result.Take(take.Value).ToAsyncEnumerable();
            }
            return result.ToAsyncEnumerable();
        }
        public Task<ftJournalME> GetLastEntryAsync()
        {
            var result = Data.Select(x => x.Value).Where(x => x.JournalType == (long) JournalTypes.JournalME).OrderByDescending(x => x.Number).FirstOrDefault();
            return Task.FromResult(result);
        }

        public IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId)
        {
            var result = Data.Select(x => x.Value).Where(x => x.ftQueueItemId == queueItemId).OrderByDescending(x => x.TimeStamp);
            return result.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference)
        {
            var result = Data.Select(x => x.Value).Where(x => x.cbReference == cbReceiptReference).OrderByDescending(x => x.TimeStamp);
            return result.ToAsyncEnumerable();
        }
    }
}
