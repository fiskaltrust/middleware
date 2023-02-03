using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class JournalProcessorIT : IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorIT> _logger;

        public JournalProcessorIT(
            ILogger<JournalProcessorIT> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
