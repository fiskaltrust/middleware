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
        if (voidRequest.cbChargeItems == null || originalRequest.cbChargeItems == null)
        {
            return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
        }

        if (voidRequest.cbChargeItems.Count != originalRequest.cbChargeItems.Count)
        {
            return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
        }

        if (voidRequest.cbPayItems.Count != originalRequest.cbPayItems.Count)
        {
            return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
        }

        var (flowControl, value) = RefundValidator.CompareReceiptRequest(originalReceiptReference, voidRequest, originalRequest);
        if (!flowControl)
        {
            return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
        }

        for (int i = 0; i < voidRequest.cbChargeItems.Count; i++)
        {
            var refundItem = voidRequest.cbChargeItems[i];
            var originalItem = originalRequest.cbChargeItems[i];

            // For void operations, quantity and amount must be the exact opposite sign of the original.
            var quantitySignMismatch = !AreOppositeWithTolerance(originalItem.Quantity, refundItem.Quantity, 0.001m);
            var amountSignMismatch = !AreOppositeWithTolerance(originalItem.Amount, refundItem.Amount, 0.01m);
            if (quantitySignMismatch || amountSignMismatch)
            {
                return $"{ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference)} (Field: {BuildSignMismatchField("ChargeItem", quantitySignMismatch, amountSignMismatch)})";
            }

            (flowControl, value) = RefundValidator.CompareChargeItems(originalReceiptReference, refundItem, originalItem);
            if (!flowControl)
            {
                return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
            }
        }

        for (int i = 0; i < voidRequest.cbPayItems.Count; i++)
        {
            var refundItem = voidRequest.cbPayItems[i];
            var originalItem = originalRequest.cbPayItems[i];

            var quantitySignMismatch = !AreOppositeWithTolerance(originalItem.Quantity, refundItem.Quantity, 0.001m);
            var amountSignMismatch = !AreOppositeWithTolerance(originalItem.Amount, refundItem.Amount, 0.01m);
            if (quantitySignMismatch || amountSignMismatch)
            {
                return $"{ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference)} (Field: {BuildSignMismatchField("PayItem", quantitySignMismatch, amountSignMismatch)})";
            }

            (flowControl, value) = RefundValidator.ComparePayItems(originalReceiptReference, refundItem, originalItem);
            if (!flowControl)
            {
                return ErrorMessagesPT.EEEE_VoidItemsMismatch(originalReceiptReference);
            }
        }
        return null; // Validation passed
    }

    private static bool AreOppositeWithTolerance(decimal original, decimal candidate, decimal tolerance)
    {
        if (Math.Abs(original) <= tolerance)
        {
            return Math.Abs(candidate) <= tolerance;
        }

        return Math.Abs(original + candidate) <= tolerance;
    }

    private static string BuildSignMismatchField(string itemType, bool quantityMismatch, bool amountMismatch)
    {
        if (quantityMismatch && amountMismatch)
        {
            return $"{itemType}.QuantitySign, {itemType}.AmountSign";
        }

        if (quantityMismatch)
        {
            return $"{itemType}.QuantitySign";
        }

        return $"{itemType}.AmountSign";
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
