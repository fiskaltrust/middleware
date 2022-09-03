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
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    internal class ZeroReceiptCommand : RequestCommand
    {
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;

        public override string ReceiptName => "Zero receipt";

        public ZeroReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger, IReadOnlyQueueItemRepository queueItemRepository)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger)
        {
            _queueItemRepository = queueItemRepository;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems?.Count() != 0 || request.cbPayItems?.Count() != 0)
            {
                throw new ArgumentException("Zero receipts must not contain charge- or payitems.");
            }
            var response = CreateReceiptResponse(request, queueItem, queueAT, queue);

            var actionJournals = new List<ftActionJournal>
            {
                CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} requests zero receipt of Queue {queueAT.ftQueueATId}")
            };

            var (receiptIdentification, ftStateData, _, signatureItems, journalAT) = await SignReceiptAsync(queueAT, request, response.ftReceiptIdentification, response.ftReceiptMoment, queueItem.ftQueueItemId);
            response.ftSignatures = response.ftSignatures.Concat(signatureItems).ToArray();
            response.ftReceiptIdentification = receiptIdentification;
            response.ftStateData = ftStateData;

            // Recover from late-signing mode
            if (queueAT.UsedMobileCount > 0 && queueAT.UsedMobileQueueItemId.HasValue)
            {
                var fromReceipt = await GetReceiptIdentificationFromQueueItem(queueAT.UsedMobileQueueItemId.Value);
                var toReceipt = response.ftReceiptIdentification;

                signatureItems.Add(new SignaturItem { Caption = "Mobil-Nacherfassung", Data = $"Von {fromReceipt} bis {toReceipt} ", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation });
                actionJournals.Add(CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueAT.ftQueueATId} from used-moblie mode. Closing chain of mobile receipts from {fromReceipt} to {toReceipt}."));

                queueAT.UsedMobileCount = 0;
                queueAT.UsedMobileMoment = null;
                queueAT.UsedMobileQueueItemId = null;

                if ((response.ftState & 0x0008) != 0 && queueAT.UsedFailedCount == 0)
                {
                    response.ftState -= 0x0008;
                }
            }

            // Recover from failed mode
            if (journalAT != null && queueAT.UsedFailedCount > 0)
            {
                var fromReceipt = await GetReceiptIdentificationFromQueueItem(queueAT.UsedFailedQueueItemId.Value);
                var toReceipt = response.ftReceiptIdentification;

                signatureItems.Add(new SignaturItem() { Caption = "Ausfall-Nacherfassung", Data = $"Von {fromReceipt} bis {toReceipt}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation });
                actionJournals.Add(CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueAT.ftQueueATId} from used-failed mode. Closing chain of failed receipts from {fromReceipt} to {toReceipt}."));

                if (queueAT.UsedFailedMomentMin.HasValue && queueAT.UsedFailedMomentMax.HasValue && queueAT.UsedFailedMomentMax.Value.Subtract(queueAT.UsedFailedMomentMin.Value).TotalHours > 48)
                {
                    actionJournals.Add(ATFONRegistrationHelper.CreateQueueDeactivationJournal(queue, queueAT, queueItem, journalAT, isStopReceipt: false));
                    actionJournals.Add(ATFONRegistrationHelper.CreateQueueActivationJournal(queue, queueAT, queueItem, journalAT, isStartReceipt: false));
                }

                queueAT.UsedFailedCount = 0;
                queueAT.UsedFailedMomentMin = null;
                queueAT.UsedFailedMomentMax = null;
                queueAT.UsedFailedQueueItemId = null;

                if ((response.ftState & 0x0008) != 0 && queueAT.UsedMobileCount == 0)
                {
                    response.ftState -= 0x0008;
                }
            }

            // Recover from SSCD failed mode
            if (journalAT != null && queueAT.SSCDFailCount > 0)
            {
                var fromReceipt = await GetReceiptIdentificationFromQueueItem(queueAT.SSCDFailQueueItemId.Value);
                var toReceipt = response.ftReceiptIdentification;

                signatureItems.Add(new SignaturItem() { Caption = "Sammelbeleg", Data = $"Von {fromReceipt} bis {toReceipt}", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.AT_StorageObligation });
                actionJournals.Add(CreateActionJournal(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueAT.ftQueueATId} from SSCD-failed mode. Closing chain of failed receipts from {fromReceipt} to {toReceipt}."));

                if (queueAT.SSCDFailMessageSent.HasValue)
                {
                    actionJournals.Add(ATFONRegistrationHelper.CreateQueueActivationJournal(queue, queueAT, queueItem, journalAT, false));
                }

                queueAT.SSCDFailCount = 0;
                queueAT.SSCDFailMoment = null;
                queueAT.SSCDFailQueueItemId = null;
                queueAT.SSCDFailMessageSent = null;

                // Remove temporary SSCD-failed state
                if ((response.ftState & 0x0002) != 0)
                {
                    response.ftState -= 0x0002;
                }

                // Remove permanent SSCD-failed state
                if ((response.ftState & 0x0004) != 0)
                {
                    response.ftState -= 0x0004;
                }
            }
            var notificationSignatures = CreateNotificationSignatures(actionJournals);
            response.ftSignatures = response.ftSignatures.Extend(signatureItems).Extend(notificationSignatures);
            
            return new RequestCommandResponse
            {
                ReceiptResponse = response,
                ActionJournals = actionJournals,
                JournalAT = journalAT
            };
        }

        private async Task<string> GetReceiptIdentificationFromQueueItem(Guid queueItemId)
        {
            try
            {
                var fromQueueItem = await _queueItemRepository.GetAsync(queueItemId);
                var fromResponse = JsonConvert.DeserializeObject<ReceiptResponse>(fromQueueItem.response);
                return fromResponse.ftReceiptIdentification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while trying to resolve the QueueItem {queueItemId} to identify the beginning of the late-signing or failed mode. This might hint to a corrupted database, e.g. due to a restored backup.");
                return "#";
            }
        }
    }
}
