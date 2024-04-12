using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Extensions;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Out-of-operation receipt";

        public OutOfOperationReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse response)
        {
            ThrowIfTraining(request);

            if ((request.cbChargeItems != null && request.cbChargeItems?.Count() != 0) || (request.cbPayItems != null && request.cbPayItems?.Count() != 0))
            {
                var notZeroReceiptActionJournal = CreateActionJournal(queue, queueItem, $"Tried to deactivate {queue.ftQueueId}, but the incoming receipt is not a zero receipt.");
                _logger.LogInformation(notZeroReceiptActionJournal.Message);

                return new RequestCommandResponse
                {
                    ReceiptResponse = response,
                    ActionJournals = new() { notZeroReceiptActionJournal }
                };
            }

            var aj = CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} used to activate Queue {queueAT.ftQueueATId}.");

            var (actionJournals, journalAT) = await SignReceiptAndDisableQueueAsync(queue, queueAT, queueItem, request, response);
            actionJournals.Add(aj);

            var notificationSignatures = CreateNotificationSignatures(actionJournals);
            response.ftSignatures = response.ftSignatures.Concat(notificationSignatures).ToArray();

            return new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = actionJournals,
                JournalAT = journalAT
            };
        }

        internal async Task<(List<ftActionJournal> actionJournals, ftJournalAT journalAT)> SignReceiptAndDisableQueueAsync(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response)
        {
            var actionJournals = new List<ftActionJournal>();

            var (receiptIdentification, ftStateData, _, signatureItems, journalAT) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId, true);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;

            if (journalAT != null)
            {
                // Receipt successfully signed, deactivate Queue
                queue.StopMoment = DateTime.UtcNow;
                response.ftSignatures = response.ftSignatures.Extend(new SignaturItem
                {
                    Caption = "Stopbeleg",
                    Data = "Stopbeleg",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation
                });

                var fonDeactivateQueueActionJournal = ATFONRegistrationHelper.CreateQueueDeactivationJournal(queue, queueAT, queueItem, journalAT);
                _logger.LogInformation(fonDeactivateQueueActionJournal.Message);
                actionJournals.Add(fonDeactivateQueueActionJournal);

                IncrementMessageCount(ref queueAT);

                var fonVerifyQueueActionJournal = ATFONRegistrationHelper.CreateQueueVerificationJournal(queue, queueAT, queueItem, journalAT);
                _logger.LogInformation(fonVerifyQueueActionJournal.Message);
                actionJournals.Add(fonVerifyQueueActionJournal);

                IncrementMessageCount(ref queueAT);

                return (actionJournals, journalAT);
            }
            else
            {
                var signingFailedActionJournal = CreateActionJournal(queue, queueItem, $"Out of operation receipt failed: Could not sign stop receipt.");
                _logger.LogInformation(signingFailedActionJournal.Message);
                actionJournals.Add(signingFailedActionJournal);

                return (actionJournals, null);
            }
        }
    }
}
