using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly ILogger<InitialOperationReceiptCommand> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly IITSSCDProvider _itIsscdProvider;

        public InitialOperationReceiptCommand(ICountrySpecificSettings countrySpecificQueueSettings, IITSSCDProvider itIsscdProvider, ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryIT signatureItemFactoryIT) : base(countrySpecificQueueSettings, logger, configurationRepository)
        {
            _itIsscdProvider = itIsscdProvider;
            _logger = logger;
            _configurationRepository = configurationRepository;
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _countrySpecificQueueRepository = countrySpecificQueueSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificQueueSettings.CountryBaseState;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);
            if (queue.IsNew())
            {
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, _countryBaseState);
                var (actionJournal, signature) = await InitializeSCUAsync(queue, request, queueItem);
                queue.StartMoment = DateTime.UtcNow;

                var result = await _itIsscdProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse,
                });
                if (!result.ReceiptResponse.ftSignatures.Any())
                {
                    result.ReceiptResponse.ftSignatures = new SignaturItem[]
                    {
                        signature
                    };
                }
                return new RequestCommandResponse
                {
                    ReceiptResponse = result.ReceiptResponse,
                    ActionJournals = new List<ftActionJournal> { actionJournal }
                };
            }

            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                queueItem.ftQueueItemId, queue.IsDeactivated()
                        ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                        : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");

            _logger.LogInformation(actionJournalEntry.Message);
            return new RequestCommandResponse
            {
                ActionJournals = new List<ftActionJournal> { actionJournalEntry },
                ReceiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, _countryBaseState)
            };
        }

        protected override async Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
            var deviceInfo = await _itIsscdProvider.GetRTInfoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(scu.InfoJson))
            {
                scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
            }

            var signatureItem = _signatureItemFactoryIT.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId} Serial-Nr: {deviceInfo.SerialNumber}");
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0",
            };
            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return (actionJournal, signatureItem);
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
