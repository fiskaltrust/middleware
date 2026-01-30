using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class DocumentStatusProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;
    public DocumentStatusProvider(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    public async Task<DocumentStatusState> GetDocumentStatusStateAsync((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        // For Portugal there are a few different States per category, we should make sure that we handle them correctly
        // In addition to that we have some abstracted states
        // Portugal
        // InvoiceStatus, N (Normal), S (Self-Billing), A (Cancelled Document), R (Summary Document), F (Invoice document)
        // MovementSatus, N (Normal), T (On behalf of third parties), A (Cancelled Document), R (Summary Document), F (Invoice document)
        // WorkStatus, N (Normal), A (Cancelled Document), F (Invoice document)
        // fiskaltrust States
        // Unknown - We cannot determine the status of this receipt
        // NotReferenced - No other document references this receipt
        // Invoiced - A document of type FT or FS references this receipt
        // Voided - A document that has been voided
        // Refunded - A full refund of the document
        // PartiallyRefunded - A partial refund of the document

        var references = await GetDocumentsReferencingReceiptAsync(receipt.receiptRequest);
        if (references.Count == 0)
        {
            return new DocumentStatusState(DocumentStatus.NotReferenced);
        }

        //  I think  we only should consider the latest state for calculation
        var lastReference = references.OrderBy(x => x.receiptResponse.ftReceiptMoment).Last();
        if (lastReference.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var voidReceipt = references.First(x => x.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void));
            return new DocumentStatusState(DocumentStatus.Voided, voidReceipt, references);
        }

        if (lastReference.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var refundReceipt = references.First(x => x.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund));
            return new DocumentStatusState(DocumentStatus.Refunded, refundReceipt, references);
        }

        if (PTMappings.ExtractDocumentTypeAndUniqueIdentification(lastReference.receiptResponse.ftReceiptIdentification).documentType == "FT" || PTMappings.ExtractDocumentTypeAndUniqueIdentification(lastReference.receiptResponse.ftReceiptIdentification).documentType == "FS")
        {
            var invoicedReceipt = references.OrderBy(x => x.receiptResponse.ftReceiptMoment).LastOrDefault();
            return new DocumentStatusState(DocumentStatus.Invoiced, invoicedReceipt, references);
        }

        return new DocumentStatusState(DocumentStatus.Unknown, lastReference, references);
    }

    private async Task<List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>> GetDocumentsReferencingReceiptAsync(ReceiptRequest receiptRequest)
    {
        List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> references = new List<(ReceiptRequest, ReceiptResponse)>();
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }
            try
            {
                var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);

                if (referencedRequest == null || referencedResponse == null)
                {
                    continue;
                }

                if (!referencedResponse.ftState.IsState(State.Success))
                {
                    continue;
                }

                if (referencedRequest.cbPreviousReceiptReference == receiptRequest.cbReceiptReference)
                {
                    references.Add((referencedRequest, referencedResponse));
                }
            }
            catch
            {
                continue;
            }
        }
        return references;
    }

}
