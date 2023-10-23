using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    /// <summary>
    /// Class responsible for processing journal requests for the DEFAULT market.
    /// </summary>
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

        /// <summary>
        /// Processes journal calls, retrieves the list of cash boxes, and returns it as a journal response.
        /// </summary>
        /// <remarks>
        /// In a real market, different journals based on the JournalRequest.ftJournalType may need to be implemented.
        /// This implementation is specific to the DEFAULT market and should be adapted as needed for other markets.
        /// </remarks>
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