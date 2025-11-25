using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

/// <summary>
/// Validates void operations according to Portuguese regulations
/// </summary>
public class VoidValidator
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;

    public VoidValidator(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    /// <summary>
    /// Validates a void operation against the original receipt
    /// For void operations, the receipt must match the original exactly (no items in void receipt)
    /// </summary>
    public async Task<string?> ValidateVoidAsync(
        ReceiptRequest voidRequest,
        ReceiptRequest originalRequest,
        string originalReceiptReference)
    {
        // Void receipts should have empty charge items and pay items
        if (voidRequest.cbChargeItems?.Any() == true)
        {
            return ErrorMessagesPT.EEEE_VoidMustHaveEmptyChargeItems;
        }

        if (voidRequest.cbPayItems?.Any() == true)
        {
            return ErrorMessagesPT.EEEE_VoidMustHaveEmptyPayItems;
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Checks if a void already exists for the given receipt reference
    /// </summary>
    public async Task<bool> HasExistingVoidAsync(string receiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;

        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request))
            {
                continue;
            }

            try
            {
                var request = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                if (request != null &&
                    request.cbPreviousReceiptReference != null &&
                    request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
                {
                    var previousRef = request.cbPreviousReceiptReference.ToString();
                    if (previousRef == receiptReference)
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
