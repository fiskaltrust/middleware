using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class SignProcessorES : IMarketSpecificSignProcessor
    {
        private readonly ILogger<SignProcessorES> _logger;

        public SignProcessorES(
            ILogger<SignProcessorES> logger)
        {
            _logger = logger;
        }

        public Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => throw new NotImplementedException();
        public Task FinalTask(ftQueue queue, ftQueueItem queueItem, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor) { return null; }
    }
}
