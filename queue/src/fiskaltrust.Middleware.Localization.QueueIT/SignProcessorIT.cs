using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        private readonly ILogger<SignProcessorIT> _logger;

        public SignProcessorIT(            
            ILogger<SignProcessorIT> logger)
        {
            _logger = logger;
        }

        public Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            throw new NotImplementedException();
        }
    }
}
