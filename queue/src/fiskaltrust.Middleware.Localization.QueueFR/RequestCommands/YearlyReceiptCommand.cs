using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class YearlyReceiptCommand : RequestCommand
    {
        public YearlyReceiptCommand(ISignatureFactoryFR signatureFactoryFR) : base(signatureFactoryFR)
        { }

        public override Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, request.GetTotals(), signaturCreationUnitFR);
                var dailyPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                var dailyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(dailyPayload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);

                var monthlyPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetMonthTotals(), queueFR.GLastHash);
                var monthlyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(monthlyPayload, "Month Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000004);

                var yearlyPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetYearTotals(), queueFR.GLastHash);
                var yearlyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(yearlyPayload, "Year Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000005);

                response.ftSignatures = response.ftSignatures.Extend(new[] { dailyTotalsSignature, monthlyTotalsSignature, yearlyTotalsSignature });

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"G{++queueFR.GNumerator}";

                var ticketPayload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, queueFR.GetDayTotals(), queueFR.GLastHash);
                
                var dailyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(ticketPayload, "Day Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000003);
                queueFR.ResetDailyTotalizers(queueItem);

                var monthlyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(ticketPayload, "Month Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000004);
                queueFR.ResetMonthlyTotalizers(queueItem);

                var yearlyTotalsSignature = _signatureFactoryFR.CreateTotalsSignatureWithoutSigning(ticketPayload, "Year Totals", SignaturItem.Formats.Text, (SignaturItem.Types) 0x4652000000000005);
                queueFR.ResetMonthlyTotalizers(queueItem);

                var perpetualTicketsSignature = _signatureFactoryFR.CreatePerpetualTotalSignature(queueFR);
                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, ticketPayload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);

                queueFR.GLastHash = hash;
                journalFR.ReceiptType = "G";
                response.ftSignatures = response.ftSignatures.Extend(new[] { dailyTotalsSignature, monthlyTotalsSignature, yearlyTotalsSignature, perpetualTicketsSignature, signatureItem });

                var actionJournal = ActionJournalFactory.Create(queue, queueItem, "Yearly closure", PayloadFactory.GetGrandTotalPayload(request, response, queueFR, signaturCreationUnitFR, queueFR.GLastHash));

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new() { actionJournal }));
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Yearly receipt must not have charge items." };
            }
            if (request.cbPayItems != null && request.cbPayItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Yearly receipt must not have pay items." };
            }
        }
    }
}
