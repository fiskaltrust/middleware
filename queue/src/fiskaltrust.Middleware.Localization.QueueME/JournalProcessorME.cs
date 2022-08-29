using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class JournalProcessorME : IJournalProcessor
    {

        public JournalProcessorME() { }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
