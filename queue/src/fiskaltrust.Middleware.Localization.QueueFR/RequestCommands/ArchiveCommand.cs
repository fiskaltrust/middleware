using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class ArchiveCommand : RequestCommand
    {
        private readonly ActionJournalFactory _actionJournalFactory;
        private readonly MiddlewareConfiguration _middlewareConfig;
        private readonly ArchiveProcessor _archiveProcessor;
        private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
        private readonly IMiddlewareJournalFRRepository _journalFRRepository;
        private readonly ILogger<ArchiveCommand> _logger;

        private readonly bool _exportEnabled;

        public ArchiveCommand(SignatureFactoryFR signatureFactoryFR, ActionJournalFactory actionJournalFactory, MiddlewareConfiguration middlewareConfig, 
            ArchiveProcessor archiveProcessor, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareReceiptJournalRepository receiptJournalRepository, IMiddlewareJournalFRRepository journalFRRepository, ILogger<ArchiveCommand> logger) : base(signatureFactoryFR)
        {
            _actionJournalFactory = actionJournalFactory;
            _middlewareConfig = middlewareConfig;
            _archiveProcessor = archiveProcessor;
            _actionJournalRepository = actionJournalRepository;
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _journalFRRepository = journalFRRepository;
            _logger = logger;

            _exportEnabled = middlewareConfig.Configuration.TryGetValue("frarchiveexport", out var exportEnabledObject)
                && bool.TryParse(exportEnabledObject.ToString(), out var exportEnabled)
                && exportEnabled;
        }

        public override async Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, request.GetTotals(), signaturCreationUnitFR);

                var dailyPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                var dailyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(dailyPayload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);

                var archivePayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetArchiveTotals(), queueFR.ALastHash);
                var archiveTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(archivePayload, "Archive Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000006);

                response.ftSignatures = response.ftSignatures.Extend(new[] { dailyTotalsSignature, archiveTotalsSignature });

                return (response, journalFR, new());
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"A{++queueFR.ANumerator}";

                (var lastActionJournalId, var lastJournalFRId, var lastReceiptJournalId, var firstContainedReceiptMoment, var firstContainedReceiptQueueItemId, var lastContainedReceiptMoment, var lastContainedReceiptQueueItemId) = await GetArchivePayloadDataAsync(queueFR);
                
                
                var payload = PayloadFactory.GetArchivePayload(request, response, queueFR, signaturCreationUnitFR, queueFR.ALastHash, lastActionJournalId, lastJournalFRId
                    ,lastReceiptJournalId, firstContainedReceiptMoment, firstContainedReceiptQueueItemId, lastContainedReceiptMoment, lastContainedReceiptQueueItemId);

                if (_exportEnabled)
                {
                    try
                    {
                        var fileName = Path.Combine(_middlewareConfig.ServiceFolder, "Exports", "ArchiveFR", $"{queueFR.ftQueueFRId}_{DateTime.Now:yyyyMMddhhmmssfff}_{queueFR.CashBoxIdentification}_{queueFR.ANumerator:00}_archive.zip");
                        await _archiveProcessor.ExportArchiveDataAsync(fileName, JsonConvert.DeserializeObject<ArchivePayload>(payload), signaturCreationUnitFR);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occured while exporting archive data for QueueItem {QueueItemId}.", queueItem.ftQueueItemId);
                    }
                }

                var ticketPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                var dailyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(ticketPayload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);
                queueFR.ResetDailyTotalizers(queueItem);

                var archivePayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetArchiveTotals(), queueFR.ALastHash);
                var archiveTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(archivePayload, "Archive Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000006);
                queueFR.ResetArchiveTotalizers(queueItem);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, ticketPayload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);

                queueFR.ALastHash = hash;
                queueFR.ALastMoment = queueItem.ftQueueMoment;
                queueFR.ALastQueueItemId = queueItem.ftQueueItemId;

                journalFR.ReceiptType = "A";
                response.ftSignatures = response.ftSignatures.Extend(new[] { dailyTotalsSignature, archiveTotalsSignature, signatureItem });

                var actionJournal = _actionJournalFactory.Create(queue, queueItem, "Archive requested", payload);

                return (response, journalFR, new() { actionJournal });
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Archive receipt must not have charge items." };
            }
            if (request.cbPayItems != null && request.cbPayItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Archive receipt must not have pay items." };
            }
        }

        private async Task<(Guid? lastActionJournalId, Guid? lastJournalFRId, Guid? lastReceiptJournalId, DateTime? firstContainedReceiptMoment, Guid? firstContainedReceiptQueueItemId, DateTime? lastContainedReceiptMoment, Guid? lastContainedReceiptQueueItemId)> GetArchivePayloadDataAsync(ftQueueFR queueFR)
        {
            // TODO: Handle case when archive receipt is the first one sent ever

            var lastActionJournal = await _actionJournalRepository.GetWithLastTimestampAsync();
            var lastJournalFR = await _journalFRRepository.GetWithLastTimestampAsync();
            var lastReceiptJournal = await _receiptJournalRepository.GetWithLastTimestampAsync();
            var lastQueueItem = await _queueItemRepository.GetAsync(lastReceiptJournal.ftQueueItemId);
            var lastResponse = !string.IsNullOrEmpty(lastQueueItem.response) ? JsonConvert.DeserializeObject<ReceiptResponse>(lastQueueItem.response) : null;

            if (queueFR.ALastQueueItemId.HasValue) // Not the first archive receipt
            {
                // Technically, it could happen that this QueueItem's response is empty, but the chances are very low. Ignoring that for now, therefore.
                var previousArchiveQueueItem = await _queueItemRepository.GetAsync(queueFR.ALastQueueItemId.Value);
                var previousArchiveResponse = JsonConvert.DeserializeObject<ReceiptResponse>(previousArchiveQueueItem.response);

                return (lastActionJournal?.ftActionJournalId, lastJournalFR?.ftJournalFRId, lastReceiptJournal?.ftReceiptJournalId, previousArchiveResponse.ftReceiptMoment,
                    previousArchiveQueueItem.ftQueueItemId, lastResponse?.ftReceiptMoment, lastQueueItem.ftQueueItemId);
            }
            else // First archive receipt
            {
                var firstReceiptJournal = await _receiptJournalRepository.GetByReceiptNumber(1);
                var firstQueueItem = await _queueItemRepository.GetAsync(firstReceiptJournal.ftQueueItemId);
                var firstResponse = !string.IsNullOrEmpty(firstQueueItem.response) ? JsonConvert.DeserializeObject<ReceiptResponse>(firstQueueItem.response) : null;

                return (lastActionJournal?.ftActionJournalId, lastJournalFR?.ftJournalFRId, lastReceiptJournal?.ftReceiptJournalId, firstResponse?.ftReceiptMoment,
                   firstQueueItem.ftQueueItemId, lastResponse.ftReceiptMoment, lastQueueItem.ftQueueItemId);
            }

            


            //Guid? firstArchiveReceiptQueueItemId = null;
            //DateTime? firstArchiveReceiptMoment = null;
            //Guid? lastArchiveReceiptQueueItemId = null;
            //DateTime? lastArchiveReceiptMoment = null;
            //ftQueueItem lastAllowedQueueItem = null;
            //ArchivePayload previousArchivePayload = null;

            //do
            //{
            //    var previousArchiveQueueItemId = previousArchivePayload == null ? queueFr.ALastQueueItemId : previousArchivePayload.PreviousArchiveQueueItemId;
            //    if (!previousArchiveQueueItemId.HasValue)
            //    {
            //        previousArchivePayload = null;
            //        break;
            //    }
            //    var previousArchive = parentStorage.QueueItem(previousArchiveQueueItemId.Value);
            //    var previousArchiveResponse = JsonConvert.DeserializeObject<ReceiptResponse>(previousArchive.response);
            //    var jwt = previousArchiveResponse.ftSignatures.Where(s => s.ftSignatureType == 0x4652000000000001).First().Data;
            //    previousArchivePayload = JsonConvert.DeserializeObject<ArchivePayload>(Encoding.UTF8.GetString(Utilities.FromBase64urlString(jwt.Split('.')[1])));
            //} while (!previousArchivePayload.LastContainedReceiptQueueItemId.HasValue);

            //if (previousArchivePayload != null && previousArchivePayload.LastContainedReceiptQueueItemId.HasValue)
            //{
            //    var firstArchiveItem = parentStorage.QueueItemTableByTimeStamp(parentStorage.QueueItem(previousArchivePayload.LastContainedReceiptQueueItemId.Value).TimeStamp, null, 1).FirstOrDefault();
            //    firstArchiveReceiptQueueItemId = firstArchiveItem.ftQueueItemId;

            //    if (firstArchiveReceiptQueueItemId.Value.ToString() == receiptResponse.ftQueueItemID)
            //    {
            //        firstArchiveReceiptMoment = receiptResponse.ftReceiptMoment;
            //        lastAllowedQueueItem = firstArchiveItem;
            //    }
            //    else if (firstArchiveItem.response != null)
            //    {
            //        var firstArchiveReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(firstArchiveItem.response);
            //        firstArchiveReceiptMoment = firstArchiveReceiptResponse.ftReceiptMoment;
            //    }

            //    if (lastAllowedQueueItem == null)
            //    {
            //        lastAllowedQueueItem = parentStorage.QueueItemTableByTimeStamp(firstArchiveItem.TimeStamp + 1).Where(qi => qi.ftQueueMoment < firstArchiveReceiptMoment.Value.AddYears(1).Date).OrderByDescending(qi => qi.TimeStamp).FirstOrDefault();
            //    }
            //}
            //else if (queue.StartMoment.HasValue)
            //{
            //    var queueItems = parentStorage.QueueItemTableByTimeStamp();
            //    lastAllowedQueueItem = queueItems.Where(qi => qi.ftQueueMoment < queue.StartMoment.Value.AddYears(1).Date).OrderByDescending(qi => qi.TimeStamp).FirstOrDefault();

            //    if (!firstArchiveReceiptQueueItemId.HasValue)
            //    {
            //        var firstQueueItem = queueItems.OrderBy(qi => qi.TimeStamp).FirstOrDefault();
            //        firstArchiveReceiptQueueItemId = firstQueueItem?.ftQueueItemId;
            //        firstArchiveReceiptMoment = firstQueueItem?.response != null ? (DateTime?) JsonConvert.DeserializeObject<ReceiptResponse>(firstQueueItem.response).ftReceiptMoment : null;
            //    }
            //}

            ////it can be null in the following cases:
            ////  - the queue is not used for one year or more
            ////  - the Archive receipt has been requested twice (or more) in row
            ////  - the queue is not yet started (the execution should not reach this method in this case)
            ////  - the queue has no receipts
            //if (lastAllowedQueueItem != null)
            //{
            //    lastArchiveReceiptQueueItemId = lastAllowedQueueItem.ftQueueItemId;
            //    if (lastArchiveReceiptQueueItemId.Value.ToString() == receiptResponse.ftQueueItemID)
            //    {
            //        lastArchiveReceiptMoment = receiptResponse.ftReceiptMoment;
            //    }
            //    else if (lastAllowedQueueItem.response != null)
            //    {
            //        var lastArchiveReceiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(lastAllowedQueueItem.response);
            //        lastArchiveReceiptMoment = lastArchiveReceiptResponse.ftReceiptMoment;
            //    }
            //}
            //else
            //{
            //    lastArchiveReceiptQueueItemId = firstArchiveReceiptQueueItemId;
            //    lastArchiveReceiptMoment = firstArchiveReceiptMoment;
            //}
        }
    }
}
