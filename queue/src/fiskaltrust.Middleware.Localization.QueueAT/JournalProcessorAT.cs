using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;

namespace fiskaltrust.Middleware.Localization.QueueAT
{
    public class JournalProcessorAT : IJournalProcessor, IMarketSpecificJournalProcessor
    {
        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
