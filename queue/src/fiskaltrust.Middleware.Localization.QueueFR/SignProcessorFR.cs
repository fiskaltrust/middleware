using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class SignProcessorFR : IMarketSpecificSignProcessor
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IActionJournalRepository _actionJournalRepository;
        private readonly RequestCommandFactory _requestCommandFactory;
        private readonly SignatureFactoryFR _signatureFactoryFR;

        public SignProcessorFR(IConfigurationRepository configurationRepository, IActionJournalRepository actionJournalRepository,
            RequestCommandFactory requestCommandFactory, SignatureFactoryFR signatureFactoryFR)
        {
            _configurationRepository = configurationRepository;
            _actionJournalRepository = actionJournalRepository;
            _requestCommandFactory = requestCommandFactory;
            _signatureFactoryFR = signatureFactoryFR;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueFR = await _configurationRepository.GetQueueFRAsync(queueItem.ftQueueId).ConfigureAwait(false);

            (var receiptResponse, var actionJournals) = await PerformReceiptRequest(request, queueItem, queue, queueFR).ConfigureAwait(false);

            await _configurationRepository.InsertOrUpdateQueueFRAsync(queueFR).ConfigureAwait(false);

            return (receiptResponse, actionJournals);
        }

        private async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> PerformReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueFR queueFR)
        {
            var scuFR = await _configurationRepository.GetSignaturCreationUnitFRAsync(queueFR.ftSignaturCreationUnitFRId);
            var command = _requestCommandFactory.Create(request.ftReceiptCase);

            var stateErrors = RequestValidation.ValidateQueueState(request, queue, queueFR);
            if (stateErrors.Any())
            {
                var defaultResponse = command.CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                defaultResponse.ftState |= 0x1;

                return (defaultResponse, new List<ftActionJournal> { ActionJournalFactory.Create(queue, queueItem, stateErrors.First().Message, null) });
            }

            var requestErrors = RequestValidation.ValidateReceiptItems(request)
                .Concat(command.Validate(queue, queueFR, request, queueItem));

            if (requestErrors.Any())
            {
                await _actionJournalRepository.InsertAsync(ActionJournalFactory.Create(queue, queueItem, "The received request contained errors.", JsonConvert.SerializeObject(requestErrors.Select(x => x.Message))));
                throw new AggregateException("Could not process the receipt because the request contained errors. See inner exceptions for details.", requestErrors.Select(x => new ArgumentException(x.Message)));
            }

            (var response, var journalFR, var actionJournals) = await command.ExecuteAsync(queue, queueFR, scuFR, request, queueItem);

            if (request.HasFailedReceiptFlag())
            {
                var ajs = ProcessFailedReceiptFlag(request, queueItem, queue, queueFR);
                actionJournals.AddRange(ajs);
            }

            if (queueFR.MessageCount > 0 && !request.IsZeroReceipt())
            {
                response.ftState |= 0x40;
                response.ftSignatures = response.ftSignatures.Extend(_signatureFactoryFR.CreateMessagePendingSignature());
            }

            return (response, actionJournals);
        }

        private List<ftActionJournal> ProcessFailedReceiptFlag(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueFR queueFR)
        {
            var ajs = new List<ftActionJournal>();
            if (!queueFR.UsedFailedMomentMin.HasValue)
            {
                queueFR.UsedFailedMomentMin = request.cbReceiptMoment;
                queueFR.UsedFailedMomentMax = request.cbReceiptMoment;

                queueFR.UsedFailedQueueItemId = queueItem.ftQueueItemId;
                ajs.Add(ActionJournalFactory.Create(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} enabled mode \"UsedFailed\" of Queue {queueFR.ftQueueFRId}", null));
            }
            queueFR.UsedFailedCount++;

            if (request.cbReceiptMoment < queueFR.UsedFailedMomentMin)
            {
                queueFR.UsedFailedMomentMin = request.cbReceiptMoment;
            }
            if (request.cbReceiptMoment > queueFR.UsedFailedMomentMax)
            {
                queueFR.UsedFailedMomentMax = request.cbReceiptMoment;
            }

            return ajs;
        }
    }
}
