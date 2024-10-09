using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class JournalProcessorGR : IMarketSpecificJournalProcessor
{
    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        // TODO integrate SAFT
        throw new NotImplementedException();
    }
}
