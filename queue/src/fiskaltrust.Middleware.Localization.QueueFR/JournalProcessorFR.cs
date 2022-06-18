using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;

namespace fiskaltrust.Middleware.Queue
{
    public class JournalProcessorFR : IJournalProcessor, IMarketSpecificJournalProcessor
    {
        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            if ((0xFFFF000000000000 & (ulong) request.ftJournalType) != 0x4652000000000000)
            {
                throw new ArgumentException($"The given ftJournalType 0x{request.ftJournalType:x} is not supported in French Middleware instances.");
            }

            throw new NotImplementedException();
        }
    }
}
