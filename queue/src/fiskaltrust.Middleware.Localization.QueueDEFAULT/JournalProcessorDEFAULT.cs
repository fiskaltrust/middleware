using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class JournalProcessorDEFAULT : IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorDEFAULT> _logger;

        public JournalProcessorDEFAULT(
            ILogger<JournalProcessorDEFAULT> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
