using System;
using System.Collections.Generic;
using System.Linq;
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

        Task<ftQueueItem> GetByQueueRowAsync(long queueRow);

        Task<ftQueueItem> GetClosestPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem);

        IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem);

        IAsyncEnumerable<string> GetGroupedReceiptReferenceAsync(long? fromIncl, long? toIncl);

        IAsyncEnumerable<ftQueueItem> GetQueueItemsForReceiptReferenceAsync(string receiptReference);
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

        IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId);

        IAsyncEnumerable<ftActionJournal> GetByPriorityAfterTimestampAsync(int lowerThanPriority, long fromTimestampInclusive);
    }

    public interface IMiddlewareJournalFRRepository : IJournalFRRepository, IMiddlewareRepository<ftJournalFR>
    {
        Task<ftJournalFR> GetWithLastTimestampAsync();
        IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsAsync();
        IAsyncEnumerable<ftJournalFR> GetProcessedCopyReceiptsDescAsync();
    }

    public interface IMiddlewareJournalDERepository : IJournalDERepository
    {
        IAsyncEnumerable<ftJournalDE> GetByFileName(string fileName);
    }

    public interface IMiddlewareJournalMERepository : IJournalMERepository
    {
        IAsyncEnumerable<ftJournalME> GetByQueueItemId(Guid queueItemId);

        IAsyncEnumerable<ftJournalME> GetByReceiptReference(string cbReceiptReference);
    }

    public interface IMiddlewareJournalITRepository : IJournalITRepository
    {
        Task<ftJournalIT> GetByQueueItemId(Guid queueItemId);
    }
}

