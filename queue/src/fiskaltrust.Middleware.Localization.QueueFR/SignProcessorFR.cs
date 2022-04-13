using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
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
        private readonly ActionJournalFactory _actionJournalFactory;
        private readonly SignatureFactoryFR _signatureFactoryFR;

        public SignProcessorFR(IConfigurationRepository configurationRepository, IActionJournalRepository actionJournalRepository,
            RequestCommandFactory requestCommandFactory, ActionJournalFactory actionJournalFactory, SignatureFactoryFR signatureFactoryFR)
        {
            _configurationRepository = configurationRepository;
            _actionJournalRepository = actionJournalRepository;
            _requestCommandFactory = requestCommandFactory;
            _actionJournalFactory = actionJournalFactory;
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

                return (defaultResponse, new List<ftActionJournal> { _actionJournalFactory.Create(queue, queueItem, stateErrors.First().Message, null) });
            }

            var requestErrors = RequestValidation.ValidateReceiptItems(request)
                .Concat(command.Validate(queue, queueFR, request, queueItem));

            if (requestErrors.Any())
            {
                await _actionJournalRepository.InsertAsync(_actionJournalFactory.Create(queue, queueItem, "The received request contained errors.", JsonConvert.SerializeObject(requestErrors.Select(x => x.Message))));
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
                ajs.Add(_actionJournalFactory.Create(queue, queueItem, $"QueueItem {queueItem.ftQueueItemId} enabled mode \"UsedFailed\" of Queue {queueFR.ftQueueFRId}", null));
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

        // TODO remove and add to zero receipt
        //private void AddMessageSignatures(ref storage.ftQueueFR QueueFR, List<storage.ftActionJournal> LocalActionJournals, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse)
        //{
        //    if (QueueFR.MessageCount > 0)
        //    {
        //        var localSignatures = new List<SignaturItem>(ReceiptResponse.ftSignatures);

        //        // if the request is a Zero receipt
        //        // TODO: MOVE THIS STUFF TO ZERO RECEIPT
        //        if ((ReceiptRequest.ftReceiptCase & 0xFFFF) == 0x000F)
        //        {
        //            //return pending messages, add signatures to receiptresponse
        //            DateTime NowMoment = new DateTime(parentStorage.ActionJournalTimeStamp(QueueFR.ftQueueFRId));

        //            //find messages since messagemoment timestamp
        //            //long MessageMomentTimeStamp = DateTime.UtcNow.Subtract(new TimeSpan(72, 0, 0)).Ticks;
        //            long MessageMomentTimeStamp = 0;
        //            if (QueueFR.MessageMoment.HasValue)
        //            {
        //                //should be always set when messagecount is set. when not all messages are shown?!
        //                MessageMomentTimeStamp = QueueFR.MessageMoment.Value.Ticks;
        //            }

        //            foreach (var item in parentStorage.ActionJournalTableByTimeStamp(MessageMomentTimeStamp).Where(aj => aj.Priority < 0).ToArray().Concat(LocalActionJournals.Where(j => j.Priority < 0).ToArray()))
        //            {
        //                var signaturItem = new SignaturItem()
        //                {
        //                    Caption = item.Message,
        //                    Data = item.DataJson
        //                };

        //                signaturItem.ftSignatureFormat = (long) SignaturItem.Formats.AZTEC;
        //                signaturItem.ftSignatureType = (long) 0x4652000000000000;

        //                localSignatures.Add(signaturItem);
        //            }

        //            //if it is not a training receit, the counter will be updated
        //            if ((ReceiptRequest.ftReceiptCase & 0x0000000000020000) == 0)
        //            {
        //                QueueFR.MessageCount = parentStorage.ActionJournalTableByTimeStamp(NowMoment.Ticks).Count(aj => aj.Priority < 0) + LocalActionJournals.Count(j => j.TimeStamp >= NowMoment.Ticks && j.Priority < 0);
        //                if (QueueFR.MessageCount == 0)
        //                {
        //                    QueueFR.MessageMoment = null;
        //                }
        //                else
        //                {
        //                    QueueFR.MessageMoment = NowMoment;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //ReceiptResponse.ftState |= 0x40;

        //            //localSignatures.Add(new SignaturItem()
        //            //{
        //            //    Caption = "fiskaltrust-Message pending",
        //            //    Data = "Create a Zero receipt",
        //            //    ftSignatureFormat = (long) SignaturItem.Formats.Text,
        //            //    ftSignatureType = (long) SignaturItem.Types.Information
        //            //});
        //        }

        //        ReceiptResponse.ftSignatures = localSignatures.ToArray();
        //    }
        //}
    }
}
