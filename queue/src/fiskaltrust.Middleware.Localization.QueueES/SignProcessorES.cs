using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueES.RequestCommands;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class SignProcessorES : IMarketSpecificSignProcessor
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        private readonly IRequestCommandFactory _requestCommandFactory;
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<SignProcessorES> _logger;

        public SignProcessorES(ISSCD signingDevice, ILogger<SignProcessorES> logger, ICountrySpecificSettings countrySpecificSettings, IRequestCommandFactory requestCommandFactory, IConfigurationRepository configurationRepository)
        {
            _requestCommandFactory = requestCommandFactory;
            _configurationRepository = configurationRepository;
            _countrySpecificSettings = countrySpecificSettings;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            if (!queueIT.ftSignaturCreationUnitITId.HasValue)
            {
                throw new NullReferenceException(nameof(queueIT.ftSignaturCreationUnitITId));
            }
            var requestCommand = _requestCommandFactory.Create(request);

            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value);
            if (string.IsNullOrEmpty(scu.InfoJson) && requestCommand is not InitialOperationReceiptCommand)
            {
                throw new MissiningInitialOpException();
            }

            if (queueIT.SSCDFailCount > 0 && requestCommand is not ZeroReceiptCommandES)
            {
                var requestCommandResponse = await requestCommand.ProcessFailedReceiptRequest(_signingDevice, _logger, _countrySpecificSettings, queue, queueItem, request).ConfigureAwait(false);
                return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.ToList());
            }
            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }
    }
}
