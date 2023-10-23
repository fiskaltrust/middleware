using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class JournalProcessorES : IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorES> _logger;

        public JournalProcessorES(
            ILogger<JournalProcessorES> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
