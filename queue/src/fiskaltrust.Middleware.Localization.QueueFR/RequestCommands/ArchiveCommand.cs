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
            if (queue.ftReceiptNumerator == 0)
            {
                throw new Exception("Nothing to archive (the first receipt of a Queue cannot be an archive receipt).");
            }

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

                (var lastActionJournalId, var lastJournalFRId, var lastReceiptJournalId, var firstContainedReceiptMoment, 
                    var firstContainedReceiptQueueItemId, var lastContainedReceiptMoment, var lastContainedReceiptQueueItemId) = await GetArchivePayloadDataAsync(queueFR);
                
                
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
        }
    }
}
