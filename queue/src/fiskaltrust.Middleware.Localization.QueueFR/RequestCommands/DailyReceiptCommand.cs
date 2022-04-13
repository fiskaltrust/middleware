using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class DailyReceiptCommand : RequestCommand
    {
        private readonly ActionJournalFactory _actionJournalFactory;

        public DailyReceiptCommand(SignatureFactoryFR signatureFactoryFR, ActionJournalFactory actionJournalFactory) : base(signatureFactoryFR)
        {
            _actionJournalFactory = actionJournalFactory;
        }

        public override Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, request.GetTotals(), signaturCreationUnitFR);
                var payload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                var totalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(payload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);
                response.ftSignatures = response.ftSignatures.Extend(totalsSignature);

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"G{++queueFR.GNumerator}";

                var ticketPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                var dailyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(ticketPayload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);
                queueFR.ResetDailyTotalizers(queueItem);

                var perpetualTicketsSignature = _signatureFactoryFR.CreatePerpetualTotalSignature(queueFR);
                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, ticketPayload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);

                queueFR.GLastHash = hash;
                journalFR.ReceiptType = "G";
                response.ftSignatures = response.ftSignatures.Extend(new[] { dailyTotalsSignature, perpetualTicketsSignature, signatureItem });

                var actionJournal = _actionJournalFactory.Create(queue, queueItem, "Daily closure", PayloadFactory.GetGrandTotalPayload(request, response, queueFR, signaturCreationUnitFR, queueFR.GLastHash));

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new() { actionJournal }));
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Daily receipt must not have charge items." };
            }
            if (request.cbPayItems != null && request.cbPayItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Daily receipt must not have pay items." };
            }
        }
    }
}
