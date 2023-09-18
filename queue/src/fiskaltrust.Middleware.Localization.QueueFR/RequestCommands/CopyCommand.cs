using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class CopyCommand : RequestCommand
    {
        private readonly IJournalFRCopyPayloadRepository _copyPayloadRepository;

        public CopyCommand(ISignatureFactoryFR signatureFactoryFR,
            IJournalFRCopyPayloadRepository copyPayloadRepository) 
            : base(signatureFactoryFR)
        {
            _copyPayloadRepository = copyPayloadRepository;
        }

        public override async Task<(ReceiptResponse receiptResponse, ftJournalFR journalFR, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueFR queueFR, ftSignaturCreationUnitFR signaturCreationUnitFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.HasTrainingReceiptFlag())
            {
                var totals = request.GetTotals();
                var (response, journalFR) = CreateTrainingReceiptResponse(queue, queueFR, request, queueItem, totals, signaturCreationUnitFR);
                return (response, journalFR, new());
            }
            else
            {
                var response = CreateDefaultReceiptResponse(queue, queueFR, request, queueItem);
                response.ftReceiptIdentification += $"C{++queueFR.CNumerator}";

                var payload = PayloadFactory.GetCopyPayload(request, response, signaturCreationUnitFR, queueFR.CLastHash);

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, JsonConvert.SerializeObject(payload), "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.CLastHash = hash;
                journalFR.ReceiptType = "C";

                var duplicateCount = await _copyPayloadRepository.GetCountOfCopiesAsync(request.cbPreviousReceiptReference) + 1;

                var signatures = new[]
                {
                    signatureItem,
                    new SignaturItem() { Caption = "Duplicata", Data = $"{duplicateCount}. Duplicata de {request.cbPreviousReceiptReference}", ftSignatureFormat = (long)SignaturItem.Formats.Text, ftSignatureType = 0x4652000000000000 }
                };

                response.ftSignatures = response.ftSignatures.Extend(signatures);
                
                await _copyPayloadRepository.InsertAsync(payload.ToJournalFRCopyPayload());

                return (response, journalFR, new());
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
    }
}
