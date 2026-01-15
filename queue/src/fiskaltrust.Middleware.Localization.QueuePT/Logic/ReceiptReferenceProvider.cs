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

    public async Task<bool> HasExistingPaymentTransferAsync(string cbPreviousReceiptReference)
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
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (referencedRequest != null && referencedResponse != null &&
                    referencedRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) &&
                    referencedRequest.cbPreviousReceiptReference != null
                    && referencedResponse.ftState.IsState(State.Success))
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
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (referencedRequest != null && referencedResponse != null &&
                    referencedRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) &&
                    referencedRequest.cbPreviousReceiptReference != null
                    && referencedResponse.ftState.IsState(State.Success))
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

    public async Task<bool> HasExistingVoidAsync(string cbPreviousReceiptReference)
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
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (referencedRequest != null && referencedResponse != null &&
                    referencedRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) &&
                    referencedRequest.cbPreviousReceiptReference != null
                    && referencedResponse.ftState.IsState(State.Success))
                {
                    // Check if this void references the receipt we're checking for
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