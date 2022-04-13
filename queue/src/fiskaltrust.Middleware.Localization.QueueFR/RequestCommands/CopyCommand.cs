using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class CopyCommand : RequestCommand
    {
        public CopyCommand(SignatureFactoryFR signatureFactoryFR) : base(signatureFactoryFR) { }

        public override Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var totals = request.GetTotals();
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, totals, signaturCreationUnitFR);
                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"C{++queueFR.CNumerator}";

                var payload = PayloadFactory.GetCopyPayload(request, response, signaturCreationUnitFR, queueFR.CLastHash);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.CLastHash = hash;
                journalFR.ReceiptType = "C";

                var duplicateCount = GetCountOfExistingCopies(request.cbPreviousReceiptReference) + 1;

                var signatures = new[]
                {
                    signatureItem,
                    new SignaturItem() { Caption = "Duplicata", Data = $"{duplicateCount}. Duplicata de {request.cbPreviousReceiptReference}", ftSignatureFormat = (long)SignaturItem.Formats.Text, ftSignatureType = 0x4652000000000000 }
                };

                response.ftSignatures = response.ftSignatures.Extend(signatures);

                return Task.FromResult<(ReceiptResponse, ftJournalFR, List<ftActionJournal>)>((response, journalFR, new()));
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError {Message = $"The Copy receipt must not have charge items." };
            }
            if (request.cbPayItems != null && request.cbPayItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Copy receipt must not have pay items." };
            }
            if (string.IsNullOrWhiteSpace(request.cbPreviousReceiptReference))
            {
                yield return new ValidationError { Message = $"The Copy receipt must provide the POS System receipt reference of the receipt whose the copy has been asked." };
            }
        }

#pragma warning disable
        private int GetCountOfExistingCopies(string cbPreviousReceiptReference)
        {
            return 0;
            // TODO
            //var count = 0;
            //var copyJournals = parentStorage.JournalFRTableByType("C");
            //foreach (var journal in copyJournals)
            //{
            //    var queueItem = parentStorage.QueueItem(journal.ftQueueItemId);
            //    var response = queueItem?.response != null ? JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response) : null;
            //    if (response != null)
            //    {
            //        if (response.ftSignatures.Any(x => x.Caption == "Duplicata" && x.Data.EndsWith($"Duplicata de {cbPreviousReceiptReference}")))
            //            count++;
            //    }
            //}

            //return count;
        }
    }
}
