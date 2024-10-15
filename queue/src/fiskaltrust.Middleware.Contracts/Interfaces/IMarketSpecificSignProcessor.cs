using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface IMarketSpecificSignProcessor
    {
        Task FirstTaskAsync();
        Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem);
        Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue);
        Task FinalTaskAsync(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor);
    }
}