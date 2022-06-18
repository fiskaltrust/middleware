using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Repositories
{
    public interface IMiddlewareRepository<T>
    {
        IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive);

        IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null);
    }

    public interface IMiddlewareQueueItemRepository : IQueueItemRepository, IMiddlewareRepository<ftQueueItem>
    {
        IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId = null);

        IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem);
    }

    public interface IMiddlewareReceiptJournalRepository : IReceiptJournalRepository, IMiddlewareRepository<ftReceiptJournal>
    {
        Task<ftReceiptJournal> GetByQueueItemId(Guid ftQueueItemId);

        Task<ftReceiptJournal> GetByReceiptNumber(long ftReceiptNumber);

        Task<ftReceiptJournal> GetWithLastTimestampAsync();
    }

    public interface IMiddlewareActionJournalRepository : IActionJournalRepository, IMiddlewareRepository<ftActionJournal>
    {
        Task<ftActionJournal> GetWithLastTimestampAsync();
        
        IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive);
    }

    public interface IMiddlewareJournalFRRepository : IJournalFRRepository, IMiddlewareRepository<ftJournalFR>
    {
        Task<ftJournalFR> GetWithLastTimestampAsync();
    }

    public interface IMiddlewareJournalDERepository : IJournalDERepository
    {
        IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName);
    }
}
