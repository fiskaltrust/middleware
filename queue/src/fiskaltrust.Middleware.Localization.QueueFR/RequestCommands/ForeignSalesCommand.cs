using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class ForeignSalesCommand : RequestCommand
    {
        public ForeignSalesCommand(SignatureFactoryFR signatureFactoryFR) : base(signatureFactoryFR) { }

        public override Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            var totals = request.GetTotals();

            if (request.HasTrainingReceiptFlag())
            {
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, totals, signaturCreationUnitFR);
                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"B{++queueFR.BNumerator}";

                var payload = PayloadFactory.GetTicketPayload(request, response, signaturCreationUnitFR, totals, queueFR.BLastHash);

                queueFR.AddReceiptTotalsToBillTotals(totals);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.BLastHash = hash;
                journalFR.ReceiptType = "B";

                response.ftSignatures = response.ftSignatures.Extend(signatureItem);

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbPayItems == null || request.cbPayItems.Length != 1 || request.cbPayItems.Where(pi => pi.ftPayItemCase == 0x4652000000000011).Count() != 1)
            {
                yield return new ValidationError { Message = "The Foreign Sale receipt must have one pay item with the ftPayItemCase = 0x4652000000000011." };
            }
        }
    }
}
