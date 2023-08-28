using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Constants;
using System;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        private readonly IRequestCommandFactory _requestCommandFactory;
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<SignProcessorIT> _logger;
        private bool _loggedDisabledQueueReceiptRequest;


        public SignProcessorIT(ISSCD signingDevice, ILogger<SignProcessorIT> logger, ICountrySpecificSettings countrySpecificSettings, IRequestCommandFactory requestCommandFactory, IConfigurationRepository configurationRepository)
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
            if (!queueIT.ftSignaturCreationUnitITId.HasValue && !queue.IsActive())
            {
                throw new NullReferenceException(nameof(queueIT.ftSignaturCreationUnitITId));
            }

            var requestCommand = _requestCommandFactory.Create(request);

            if ((queue.IsNew() || queue.IsDeactivated()) && requestCommand is not InitialOperationReceiptCommand)
            {
                return await ReturnWithQueueIsDisabled(queue, queueIT, request, queueItem);
            }

            if (queueIT.SSCDFailCount > 0 && requestCommand is not ZeroReceiptCommandIT)
            {
                var requestCommandResponse = await requestCommand.ProcessFailedReceiptRequest(_signingDevice, _logger, _countrySpecificSettings, queue, queueItem, request).ConfigureAwait(false);
                return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.ToList());
            }

            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ReturnWithQueueIsDisabled(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueIT);
            var actionJournals = new List<ftActionJournal>();
            if (!_loggedDisabledQueueReceiptRequest)
            {
                actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueId {queueItem.ftQueueId} was not activated or already deactivated"
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return await Task.FromResult((receiptResponse, actionJournals)).ConfigureAwait(false);
        }

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIT)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftCashBoxIdentification = queueIT.CashBoxIdentification,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4954000000000000
            };
        }
    }
}
