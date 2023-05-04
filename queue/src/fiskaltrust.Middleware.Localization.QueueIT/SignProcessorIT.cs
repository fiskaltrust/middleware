using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories;
using fiskaltrust.storage.V0;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        private readonly IRequestCommandFactory _requestCommandFactory;
        protected readonly IConfigurationRepository _configurationRepository;

        public SignProcessorIT(IRequestCommandFactory requestCommandFactory, IConfigurationRepository configurationRepository)
        {
            _requestCommandFactory = requestCommandFactory;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            if (!queueIT.ftSignaturCreationUnitITId.HasValue)
            {
                throw new NullReferenceException(nameof(queueIT.ftSignaturCreationUnitITId));
            }
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value);
            if (string.IsNullOrEmpty(scu.InfoJson))
            {
                throw new MissiningInitialOpException();
            }
            var requestCommand = _requestCommandFactory.Create(request);
            if (queueIT.SSCDFailCount > 0 && requestCommand is not ZeroReceiptCommand)
            {
                var requestCommandResponse = await requestCommand.ProcessFailedReceiptRequest(queue, queueItem, request).ConfigureAwait(false);
                return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.ToList());
            }
            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }
    }
}
