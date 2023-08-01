using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.RequestCommands
{
    public class CopyCommand : RequestCommand
    {
        private readonly IMiddlewareJournalFRRepository _journalFRRepository;
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;

        public CopyCommand(ISignatureFactoryFR signatureFactoryFR, IMiddlewareJournalFRRepository journalFRRepository, IReadOnlyQueueItemRepository queueItemRepository) : base(signatureFactoryFR)
        {
            _journalFRRepository = journalFRRepository;
            _queueItemRepository = queueItemRepository;
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

                var (hash, signatureItem, journalFR) = _signatureFactoryFR.CreateTotalsSignature(response, queue, signaturCreationUnitFR, payload, "www.fiskaltrust.fr", SignaturItem.Formats.QR_Code, (SignaturItem.Types) 0x4652000000000001);
                queueFR.CLastHash = hash;
                journalFR.ReceiptType = "C";

                var duplicateCount = (await GetCountOfExistingCopiesAsync(request.cbPreviousReceiptReference)) + 1;

                var signatures = new[]
                {
                    signatureItem,
                    new SignaturItem() { Caption = "Duplicata", Data = $"{duplicateCount}. Duplicata de {request.cbPreviousReceiptReference}", ftSignatureFormat = (long)SignaturItem.Formats.Text, ftSignatureType = 0x4652000000000000 }
                };

                response.ftSignatures = response.ftSignatures.Extend(signatures);

                return (response, journalFR, new());
            }
        }

        public override IEnumerable<ValidationError> Validate(ftQueue queue, ftQueueFR queueFR, ReceiptRequest request, ftQueueItem queueItem)
        {
            if (request.cbChargeItems != null && request.cbChargeItems.Length > 0)
            {
                yield return new ValidationError { Message = $"The Copy receipt must not have charge items." };
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

        private async Task<int> GetCountOfExistingCopiesAsync(string cbPreviousReceiptReference)
        {
            await foreach (var copyJournal in _journalFRRepository.GetProcessedCopyReceiptsDescAsync())
            {
                var queueItem = await _queueItemRepository.GetAsync(copyJournal.ftQueueItemId);
                var response = queueItem?.response != null ? JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response) : null;
                if (response != null)
                {
                    var duplicata = response.ftSignatures.FirstOrDefault(x => x.Caption == "Duplicata" && x.Data.EndsWith($"Duplicata de {cbPreviousReceiptReference}"));

                    if (duplicata != null)
                    {
                        return int.Parse(duplicata.Data.Split('.')[0]);
                    }
                }
            }
            return 0;
        }
    }
}
