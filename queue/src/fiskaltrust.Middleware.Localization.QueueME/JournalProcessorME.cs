using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class JournalProcessorME : IJournalProcessor
    {
        private readonly ILogger<JournalProcessorME> _logger;

        public JournalProcessorME(
            ILogger<JournalProcessorME> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
