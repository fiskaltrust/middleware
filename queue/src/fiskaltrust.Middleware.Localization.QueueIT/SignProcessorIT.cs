using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        private readonly ILogger<SignProcessorIT> _logger;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly IITSSCD _client;
        protected readonly IConfigurationRepository _configurationRepository;


        public SignProcessorIT(
            ILogger<SignProcessorIT> logger,
            IRequestCommandFactory requestCommandFactory,
            IITSSCDProvider itIsscdProvider,
            IConfigurationRepository configurationRepository)
        {
            _logger = logger;
            _requestCommandFactory = requestCommandFactory;
            _client = itIsscdProvider.Instance;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            var requestCommand = _requestCommandFactory.Create(request, queueIT);
            if (requestCommand is RequestCommandIT iT)
            {
                var responseit = await iT.ExecuteAsync(_client, queue, request, queueItem, queueIT).ConfigureAwait(false);
                return (responseit.ReceiptResponse, responseit.ActionJournals.ToList());
            }
            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }
    }
}
