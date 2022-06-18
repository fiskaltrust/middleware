using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public abstract class RequestCommand
    {
        protected readonly SignatureFactoryFR _signatureFactoryFR;

        public RequestCommand(SignatureFactoryFR signatureFactoryFR)
        {
            _signatureFactoryFR = signatureFactoryFR;
        }

        public abstract Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem);

        public abstract IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem);

        public ReceiptResponse CreateDefaultReceiptResponse(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem) => new()
        {
            ftCashBoxID = request.ftCashBoxID,
            ftCashBoxIdentification = queueFR.CashBoxIdentification,
            ftQueueID = queue.ftQueueId.ToString(),
            ftQueueItemID = queueItem.ftQueueItemId.ToString(),
            ftQueueRow = queueItem.ftQueueRow,
            cbTerminalID = request.cbTerminalID,
            cbReceiptReference = request.cbReceiptReference,
            ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = 0x4652000000000000
        };

        public (ReceiptResponse receiptResponse, ftJournalFR journalFR) CreateTrainingReceiptResponse(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem, Totals totals, ftSignaturCreationUnitFR signaturCreationUnitFR)
        {
            var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
            response.ftReceiptIdentification += $"X{++queueFR.XNumerator}";

            var payload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, totals, queueFR.XLastHash);

            if (totals.Totalizer.HasValue)
            {
                queueFR.XTotalizer += totals.Totalizer.Value;
            }
            response.ftReceiptFooter = new[] { "T R A I N I N G" };

            var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
            journalFR.ReceiptType = "X";
            
            queueFR.XLastHash = hash;
            var signatures = new[]
            {
                signatureItem,
                new SignaturItem() { Caption = "mode école", Data = queue.ftQueueId.ToString(), ftSignatureFormat = (long)SignaturItem.Formats.Text, ftSignatureType = 0x4652000000000000 }
            };

            response.ftSignatures = response.ftSignatures.Extend(signatures);
            return (response, journalFR);
        }
    }
}
