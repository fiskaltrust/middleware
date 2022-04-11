using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class JournalProcessorME : IJournalProcessor
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ILogger<JournalProcessorME> _logger;
#pragma warning restore IDE0052 // Remove unread private members

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
