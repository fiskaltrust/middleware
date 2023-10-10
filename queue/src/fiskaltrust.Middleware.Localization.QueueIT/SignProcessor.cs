﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessor : IMarketSpecificSignProcessor
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly SignProcessorIT _signProcessorIT;
        private readonly LifecyclCommandProcessorIT _lifecyclCommandProcessorIT;

        public SignProcessor(IConfigurationRepository configurationRepository, SignProcessorIT signProcessorIT, LifecyclCommandProcessorIT lifecyclCommandProcessorIT)
        {
            _configurationRepository = configurationRepository;
            _signProcessorIT = signProcessorIT;
            _lifecyclCommandProcessorIT = lifecyclCommandProcessorIT;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            var receiptResponse = new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = Cases.BASE_STATE,
                ftReceiptIdentification = receiptIdentification,
                ftCashBoxIdentification = queueIT.CashBoxIdentification
            };

            if (queue.IsDeactivated())
            {
                return ReturnWithQueueIsDisabled(queue, receiptResponse, queueItem);
            }

            if (request.IsLifeCycleOperation())
            {
                if (request.IsInitialOperation() && !queue.IsNew())
                {
                    receiptResponse.SetReceiptResponseErrored("The queue is already operational. It is not allowed to send another InitOperation Receipt");
                    return (receiptResponse, new List<ftActionJournal>());
                }

                (var response, var actionJournals) = await _lifecyclCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (queue.IsNew())
            {
                return ReturnWithQueueIsNotActive(queue, receiptResponse, queueItem);
            }

            return await _signProcessorIT.ProcessAsync(request, receiptResponse, queue, queueItem).ConfigureAwait(false);

        }
        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsNotActive(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has not been activated yet."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }

        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsDisabled(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has been disabled."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }
    }
}
