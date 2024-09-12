using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class SignProcessorES : IMarketSpecificSignProcessor
    {
        public Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => throw new NotImplementedException();
        public Task FinalTaskAsync(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor) { return Task.CompletedTask; }
        public Task FirstTaskAsync() { return Task.CompletedTask; }
    }
}
