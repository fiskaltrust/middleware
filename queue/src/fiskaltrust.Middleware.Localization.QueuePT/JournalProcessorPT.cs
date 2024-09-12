using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueuePT
{
    public class JournalProcessorPT : IMarketSpecificJournalProcessor
    {
        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            // TODO integrate SAFT
            throw new NotImplementedException();
        }
    }
}
