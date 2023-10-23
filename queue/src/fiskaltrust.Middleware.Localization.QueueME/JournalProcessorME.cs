using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class JournalProcessorME : IMarketSpecificJournalProcessor
    { 
        public JournalProcessorME()
        {
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request) => throw new NotImplementedException();
    }
}
