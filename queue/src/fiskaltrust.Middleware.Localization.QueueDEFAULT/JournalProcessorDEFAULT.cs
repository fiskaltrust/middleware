using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class JournalProcessorDEFAULT : IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorDEFAULT> _logger;
        private readonly IConfigurationRepository _configurationRepository;

        public JournalProcessorDEFAULT(
            ILogger<JournalProcessorDEFAULT> logger, IConfigurationRepository configurationRepository)
        {
            _logger = logger;
            _configurationRepository = configurationRepository;
        }

        public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            var result = new { CashBoxList = await _configurationRepository.GetCashBoxListAsync() };
            yield return new JournalResponse
            {
                Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)).ToList()
            };
        }
    }
}
