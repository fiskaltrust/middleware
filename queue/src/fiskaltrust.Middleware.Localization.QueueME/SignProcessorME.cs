using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

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

        public Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            throw new NotImplementedException();
        }
    }
}
