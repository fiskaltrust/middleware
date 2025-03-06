using System;
using System.Collections.Generic;
using System.IO;
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
    public class YearlyClosingReceiptCommand : RequestCommand
    {
        public override string ReceiptName => "Yearly-closing receipt";

        public YearlyClosingReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse response)
        {
            ThrowIfTraining(request);

            if (request.HasChargeAndPayItems())
            {
                var notZeroReceiptActionJournal = CreateActionJournal(queue, queueItem, $"Tried to create a yearly receipt for {queue.ftQueueId}, but the incoming receipt is not a zero receipt.");
                _logger.LogInformation(notZeroReceiptActionJournal.Message);

                return new RequestCommandResponse
                {
                    ReceiptResponse = response,
                    ActionJournals = new() { notZeroReceiptActionJournal }
                };
            }

            var aj = CreateActionJournal(queue, queueItem, $"Annual receipt of Queue {queue.ftQueueId}. Turnover-Counter: {queueAT.ftCashTotalizer}");

            var (actionJournals, journalAT) = await SignReceiptAndProcessYearlyReceiptAsync(queue, queueAT, queueItem, request, response);
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

        internal async Task<(List<ftActionJournal> actionJournals, ftJournalAT journalAT)> SignReceiptAndProcessYearlyReceiptAsync(ftQueue queue, ftQueueAT queueAT, ftQueueItem queueItem, ReceiptRequest request, ReceiptResponse response)
        {
            var actionJournals = new List<ftActionJournal>();

            var (receiptIdentification, ftStateData, _, signatureItems, journalAT, isSigned) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId, isZeroReceipt: true);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;

            if (isSigned)
            {
                // Receipt successfully signed, process yearly receipt
                response.ftSignatures = response.ftSignatures.Extend(new SignaturItem
                {
                    Caption = $"Jahresbeleg",
                    Data = $"{queueAT.ftCashTotalizer:0.00}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation
                });

                var fonVerifyQueueActionJournal = ATFONRegistrationHelper.CreateQueueVerificationJournal(queue, queueAT, queueItem, journalAT);
                _logger.LogInformation(fonVerifyQueueActionJournal.Message);
                actionJournals.Add(fonVerifyQueueActionJournal);

                IncrementMessageCount(ref queueAT);

                // TODO: It's not super smart IMO to mix cbReceiptMoment and DateTime.UtcNow here, but it was like this before - might make sense to change this though.
                queueAT.LastSettlementMoment = request.cbReceiptMoment;
                queueAT.LastSettlementMonth += 1;
                if ((response.ftState & 0x20L) > 0)
                {
                    response.ftState -= 0x20L;
                }
                if ((response.ftState & 0x10L) > 0)
                {
                    response.ftState -= 0x10L;
                }

                return (actionJournals, journalAT);
            }
            else
            {
                var signingFailedActionJournal = CreateActionJournal(queue, queueItem, $"Yearly receipt failed: Could not sign receipt.");
                _logger.LogInformation(signingFailedActionJournal.Message);
                actionJournals.Add(signingFailedActionJournal);

                return (actionJournals, null);
            }
        }
    }
}
