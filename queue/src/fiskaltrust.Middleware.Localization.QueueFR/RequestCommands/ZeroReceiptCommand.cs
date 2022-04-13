using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
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

        public ZeroReceiptCommand(SignatureFactoryFR signatureFactoryFR, ActionJournalFactory actionJournalFactory, IReadOnlyQueueItemRepository queueItemRepository) : base(signatureFactoryFR)
        {
            _actionJournalFactory = actionJournalFactory;
            _queueItemRepository = queueItemRepository;
        }

        public override Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var totals = request.GetTotals();

                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, totals, signaturCreationUnitFR);
                response.ftSignatures = response.ftSignatures.Extend(GetPendingMessageSignatures(queueFR, request, response));
                
                var actionJournals = ResetFailedMode(request, response, queue, queueFR, queueItem);

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, actionJournals));
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftSignatures = response.ftSignatures.Extend(GetPendingMessageSignatures(queueFR, request, response));
                response.ftReceiptIdentification += $"G{++queueFR.BNumerator}";

                var payload = PayloadFactory.GetGrandTotalPayload(request, response, queueFR, signaturCreationUnitFR, queueFR.GLastHash);

                var actionJournals = ResetFailedMode(request, response, queue, queueFR, queueItem);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.GLastHash = hash;
                journalFR.ReceiptType = "G";

                response.ftSignatures = response.ftSignatures.Extend(signatureItem);

                actionJournals.Add(_actionJournalFactory.Create(queue, queueItem, "Zero receipt", payload));
                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, actionJournals));
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

#pragma warning disable
        private List<SignaturItem> GetPendingMessageSignatures(ftQueueFR queueFR, ReceiptRequest request, ReceiptResponse response)
        {
            return new();
            // TODO
            //if (queueFR.MessageCount > 0)
            //{
            //    var localSignatures = new List<SignaturItem>(response.ftSignatures);

            //    //return pending messages, add signatures to receiptresponse
            //    DateTime NowMoment = new DateTime(parentStorage.ActionJournalTimeStamp(queueFR.ftQueueFRId));

            //    //find messages since messagemoment timestamp
            //    //long MessageMomentTimeStamp = DateTime.UtcNow.Subtract(new TimeSpan(72, 0, 0)).Ticks;
            //    long MessageMomentTimeStamp = 0;
            //    if (queueFR.MessageMoment.HasValue)
            //    {
            //        //should be always set when messagecount is set. when not all messages are shown?!
            //        MessageMomentTimeStamp = queueFR.MessageMoment.Value.Ticks;
            //    }

            //    foreach (var item in parentStorage.ActionJournalTableByTimeStamp(MessageMomentTimeStamp).Where(aj => aj.Priority < 0).ToArray().Concat(LocalActionJournals.Where(j => j.Priority < 0).ToArray()))
            //    {
            //        var signaturItem = new SignaturItem()
            //        {
            //            Caption = item.Message,
            //            Data = item.DataJson
            //        };

            //        signaturItem.ftSignatureFormat = (long) SignaturItem.Formats.AZTEC;
            //        signaturItem.ftSignatureType = (long) 0x4652000000000000;

            //        localSignatures.Add(signaturItem);
            //    }

            //    //if it is not a training receit, the counter will be updated
            //    if ((request.ftReceiptCase & 0x0000000000020000) == 0)
            //    {
            //        queueFR.MessageCount = parentStorage.ActionJournalTableByTimeStamp(NowMoment.Ticks).Count(aj => aj.Priority < 0) + LocalActionJournals.Count(j => j.TimeStamp >= NowMoment.Ticks && j.Priority < 0);
            //        if (queueFR.MessageCount == 0)
            //        {
            //            queueFR.MessageMoment = null;
            //        }
            //        else
            //        {
            //            queueFR.MessageMoment = NowMoment;
            //        }
            //    }

            //    response.ftSignatures = localSignatures.ToArray();
            //}
        }
    }
}
