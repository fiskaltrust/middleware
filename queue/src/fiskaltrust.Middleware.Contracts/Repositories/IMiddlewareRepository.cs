using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Repositories
{
    public interface IMiddlewareRepository<T>
    {
        IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive);

        IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null);
    }

    public interface IMiddlewareQueueItemRepository : IQueueItemRepository
    {
        IAsyncEnumerable<ftQueueItem> GetByReceiptReferenceAsync(string cbReceiptReference, string cbTerminalId = null);

        IAsyncEnumerable<ftQueueItem> GetPreviousReceiptReferencesAsync(ftQueueItem ftQueueItem);

        IAsyncEnumerable<ftQueueItem> GetQueueItemsAfterQueueItem(ftQueueItem ftQueueItem);
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

    public interface IMiddlewareActionJournalRepository : IActionJournalRepository
    {
        IAsyncEnumerable<ftActionJournal> GetByQueueItemId(Guid queueItemId);
    }
}

