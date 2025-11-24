using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class ReceiptReferenceProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;
    public ReceiptReferenceProvider(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    public async Task<List<(ReceiptRequest, ReceiptResponse)>> GetReceiptReferencesIfNecessaryAsync(ProcessCommandRequest request)
    {
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
        }
        return receiptReferences;
    }

    private async Task<List<(ReceiptRequest, ReceiptResponse)>> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        if (request.cbPreviousReceiptReference is null)
        {
            return new List<(ReceiptRequest, ReceiptResponse)>();
        }

        return await request.cbPreviousReceiptReference.MatchAsync(
            async single => [await LoadReceiptReferencesToResponse(request, receiptResponse, single)],
            async group =>
            {
                var references = new List<(ReceiptRequest, ReceiptResponse)>();
                foreach (var reference in group)
                {
                    var item = await LoadReceiptReferencesToResponse(request, receiptResponse, reference);
                    references.Add(item);
                }
                return references;
            }
        );

    }

    private async Task<(ReceiptRequest, ReceiptResponse)> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse, string cbPreviousReceiptReferenceString)
    {
        var queueItems = (await _readOnlyQueueItemRepository.Value).GetByReceiptReferenceAsync(cbPreviousReceiptReferenceString, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(existingQueueItem.request);
            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            if (referencedResponse != null && referencedRequest != null)
            {

                return (referencedRequest, referencedResponse);
            }
            else
            {
                throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
            }
        }
        throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
    }

    public async Task<bool> HasExistingRefundAsync(string cbPreviousReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        
        // Unfortunately, there's no direct way to query by cbPreviousReceiptReference,
        // so we need to check queue items that might reference this receipt.
        // We use GetQueueItemsForReceiptReferenceAsync which returns items with this cbReceiptReference
        // but we actually need to check all items to find refunds that reference it via cbPreviousReceiptReference
        
        // A more efficient approach: check recent items (assuming refunds come after original receipts)
        var lastItem = await queueItemRepository.GetLastQueueItemAsync();
        if (lastItem == null)
        {
            return false;
        }
        
        // Search through queue items to find any refund that references this receipt
        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }
 
            try
            {
                var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                if (referencedRequest != null && 
                    referencedRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) &&
                    referencedRequest.cbPreviousReceiptReference != null)
                {
                    // Check if this refund references the receipt we're checking for
                    var previousRef = referencedRequest.cbPreviousReceiptReference.SingleValue;
                    if (previousRef == cbPreviousReceiptReference)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore deserialization errors and continue
                continue;
            }
        }
        
        return false;
    }
}