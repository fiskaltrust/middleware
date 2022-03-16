using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class SignProcessorME : IMarketSpecificSignProcessor
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IRequestCommandFactory _requestCommandFactory;

        public SignProcessorME(
            IConfigurationRepository configurationRepository,
            IRequestCommandFactory requestCommandFactory)
        {
            _configurationRepository = configurationRepository;
            _requestCommandFactory = requestCommandFactory;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var response = await _requestCommandFactory.Create(request).ExecuteAsync(queue, request, queueItem);

            return (response.ReceiptResponse, response.ActionJournals);
        }
    }
}
