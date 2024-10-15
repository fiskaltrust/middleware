using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class JournalProcessorES : IMarketSpecificJournalProcessor
{
    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        throw new NotImplementedException();
    }
}
