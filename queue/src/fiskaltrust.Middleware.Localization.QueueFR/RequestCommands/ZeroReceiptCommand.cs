using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class ZeroReceiptCommand : RequestCommand
    {
        private readonly ActionJournalFactory _actionJournalFactory;
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;
        private readonly IMiddlewareActionJournalRepository _actionJournalRepository;

        public ZeroReceiptCommand(SignatureFactoryFR signatureFactoryFR, ActionJournalFactory actionJournalFactory, IReadOnlyQueueItemRepository queueItemRepository,
            IMiddlewareActionJournalRepository actionJournalRepository) : base(signatureFactoryFR)
        {
            _actionJournalFactory = actionJournalFactory;
            _queueItemRepository = queueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public override async Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var totals = request.GetTotals();

                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, totals, signaturCreationUnitFR);
                response.ftSignatures = response.ftSignatures.Extend(await GetPendingMessageSignatures(queueFR, request));

                var actionJournals = ResetFailedMode(request, response, queue, queueFR, queueItem);

                return (response, journalFR, actionJournals);
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftSignatures = response.ftSignatures.Extend(await GetPendingMessageSignatures(queueFR, request));
                response.ftReceiptIdentification += $"G{++queueFR.BNumerator}";

                var payload = PayloadFactory.GetGrandTotalPayload(request, response, queueFR, signaturCreationUnitFR, queueFR.GLastHash);

                var actionJournals = ResetFailedMode(request, response, queue, queueFR, queueItem);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.GLastHash = hash;
                journalFR.ReceiptType = "G";

                response.ftSignatures = response.ftSignatures.Extend(signatureItem);

                actionJournals.Add(_actionJournalFactory.Create(queue, queueItem, "Zero receipt", payload));
                return (response, journalFR, actionJournals);
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Zero receipt must not have charge items." };
            }
            if (request.cbPayItems != null && request.cbPayItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Zero receipt must not have pay items." };
            }
        }

        private List<ftActionJournal> ResetFailedMode(ReceiptRequest request, ReceiptResponse response, ftQueue queue, ftQueueFR queueFR, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>();
            if (queueFR.UsedFailedCount > 0)
            {
                //try to recover from usedfailed mode
                var localSignatures = new List<SignaturItem>(response.ftSignatures);

                var fromReceipt = "#";
                try
                {
                    var fromQueueItem = _queueItemRepository.GetAsync(queueFR.UsedFailedQueueItemId.Value).Result;
                    var fromResponse = JsonConvert.DeserializeObject<ReceiptResponse>(fromQueueItem.response);
                    fromReceipt = fromResponse.ftReceiptIdentification;
                }
                catch (Exception x)
                {
                    if (!request.HasTrainingReceiptFlag())
                    {
                        actionJournals.Add(_actionJournalFactory.Create(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} error on resolving ftReceiptIdentification of queue item {queueFR.UsedFailedQueueItemId} where used-failed beginns: {x.Message}.", null));
                    }
                }
                var toReceipt = response.ftReceiptIdentification;

                localSignatures.Add(new SignaturItem() { Caption = "Failure registered", Data = $"From {fromReceipt} to {toReceipt} ", ftSignatureFormat = (long) SignaturItem.Formats.Text, ftSignatureType = (long) SignaturItem.Types.Information });
                response.ftSignatures = response.ftSignatures.Extend(_signatureFactoryFR.CreateFailureRegisteredSignature(fromReceipt, toReceipt));

                //if it is not a training receit, the counter will be updated
                if ((request.ftReceiptCase & 0x0000000000020000) == 0)
                {
                    actionJournals.Add(_actionJournalFactory.Create(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} recovered Queue {queueFR.ftQueueFRId} from used-failed mode. closing chain of failed receipts from {fromReceipt} to {toReceipt}.", null));

                    //reset used-fail mode
                    queueFR.UsedFailedCount = 0;
                    queueFR.UsedFailedMomentMin = null;
                    queueFR.UsedFailedMomentMax = null;
                    queueFR.UsedFailedQueueItemId = null;
                }

                //remove used-fail state from response-state
                if ((response.ftState & 0x0008) != 0)
                {
                    //remove used-failed state
                    response.ftState -= 0x0008;
                }
            }

            return actionJournals;
        }

        private async Task<List<SignaturItem>> GetPendingMessageSignatures(ftQueueFR queueFR, ReceiptRequest request)
        {
            var localSignatures = new List<SignaturItem>();
            if (queueFR.MessageCount > 0)
            {

                long messageMoment = 0;
                if (queueFR.MessageMoment.HasValue)
                {
                    messageMoment = queueFR.MessageMoment.Value.Ticks;
                }

                await foreach (var item in _actionJournalRepository.GetByPriorityAfterTimestampAsync(lowerThanPriority: 0, messageMoment))
                {
                    var signaturItem = new SignaturItem
                    {
                        Caption = item.Message,
                        Data = item.DataJson,
                        ftSignatureFormat = (long) SignaturItem.Formats.AZTEC,
                        ftSignatureType = 0x4652000000000000
                    };

                    localSignatures.Add(signaturItem);
                }

                //if it is not a training receit, the counter will be updated
                if ((request.ftReceiptCase & 0x0000000000020000) == 0)
                {
                    queueFR.MessageCount = 0;
                    queueFR.MessageMoment = null;
                }
            }

            return localSignatures;
        }
    }
}
