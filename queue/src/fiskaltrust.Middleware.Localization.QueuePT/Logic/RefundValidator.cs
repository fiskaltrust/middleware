using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

/// <summary>
/// Validates refund operations according to Portuguese regulations
/// </summary>
public class RefundValidator
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;

    public RefundValidator(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    /// <summary>
    /// Validates a full refund against the original invoice
    /// </summary>
    public async Task<string?> ValidateFullRefundAsync(
        ReceiptRequest refundRequest,
        ReceiptRequest originalRequest,
        string originalReceiptReference)
    {
        if (refundRequest.cbChargeItems == null || originalRequest.cbChargeItems == null)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
        }

        // Build a dictionary of original items by product number/description for comparison
        var originalItems = BuildItemDictionary(originalRequest.cbChargeItems);
        var refundItems = BuildItemDictionary(refundRequest.cbChargeItems);

        // Check if all original items are present in the refund with correct quantities and amounts
        foreach (var (key, originalItem) in originalItems)
        {
            if (!refundItems.TryGetValue(key, out var refundItem))
            {
                return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
            }

            // Compare quantities (considering absolute values for refunds)
            var originalQuantity = originalItem.Quantity;
            var refundQuantity = Math.Abs(refundItem.Quantity);

            if (Math.Abs(originalQuantity - refundQuantity) > 0.001m)
            {
                return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
            }

            // Compare amounts (considering absolute values for refunds)
            var originalAmount = originalItem.Amount;
            var refundAmount = Math.Abs(refundItem.Amount);

            if (Math.Abs(originalAmount - refundAmount) > 0.01m)
            {
                return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
            }
        }

        // Check if there are any extra items in the refund that weren't in the original
        foreach (var key in refundItems.Keys)
        {
            if (!originalItems.ContainsKey(key))
            {
                return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
            }
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Validates a partial refund to ensure:
    /// 1. All items have the refund flag
    /// 2. The refund doesn't exceed the original quantities/amounts
    /// </summary>
    public async Task<string?> ValidatePartialRefundAsync(
        ReceiptRequest refundRequest,
        ReceiptRequest originalRequest,
        string originalReceiptReference)
    {
        if (refundRequest.cbChargeItems == null)
        {
            return null; // No items to validate
        }

        // Check 1: All items must have the refund flag (no mixing)
        var hasRefundItems = false;
        var hasNonRefundItems = false;

        foreach (var item in refundRequest.cbChargeItems)
        {
            if (item.IsRefund())
            {
                hasRefundItems = true;
            }
            else
            {
                hasNonRefundItems = true;
            }
        }

        if (hasRefundItems && hasNonRefundItems)
        {
            return ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed;
        }

        if (!hasRefundItems)
        {
            return null; // This is not a partial refund, no validation needed
        }

        // Check 2: Load all existing refunds for this receipt and validate totals
        var existingRefunds = await LoadExistingRefundsAsync(originalReceiptReference);
        
        // Build dictionaries for comparison
        var originalItems = BuildItemDictionary(originalRequest.cbChargeItems ?? []);
        var currentRefundItems = BuildItemDictionary(refundRequest.cbChargeItems);

        // Calculate total refunded quantities and amounts per product (including this refund)
        var totalRefundedByProduct = new Dictionary<string, (decimal Quantity, decimal Amount)>();

        // Add existing refunds
        foreach (var existingRefund in existingRefunds)
        {
            foreach (var item in existingRefund.cbChargeItems ?? [])
            {
                var key = GetItemKey(item);
                var quantity = Math.Abs(item.Quantity);
                var amount = Math.Abs(item.Amount);

                if (totalRefundedByProduct.ContainsKey(key))
                {
                    var existing = totalRefundedByProduct[key];
                    totalRefundedByProduct[key] = (existing.Quantity + quantity, existing.Amount + amount);
                }
                else
                {
                    totalRefundedByProduct[key] = (quantity, amount);
                }
            }
        }

        // Add current refund items
        foreach (var (key, item) in currentRefundItems)
        {
            var quantity = Math.Abs(item.Quantity);
            var amount = Math.Abs(item.Amount);

            if (totalRefundedByProduct.ContainsKey(key))
            {
                var existing = totalRefundedByProduct[key];
                totalRefundedByProduct[key] = (existing.Quantity + quantity, existing.Amount + amount);
            }
            else
            {
                totalRefundedByProduct[key] = (quantity, amount);
            }
        }

        // Validate that refunds don't exceed originals
        foreach (var (key, refundedTotals) in totalRefundedByProduct)
        {
            if (!originalItems.TryGetValue(key, out var originalItem))
            {
                continue; // Item not in original, skip validation
            }

            // Check quantity
            if (refundedTotals.Quantity > originalItem.Quantity + 0.001m)
            {
                var productNumber = originalItem.ProductNumber ?? originalItem.Description ?? key;
                return ErrorMessagesPT.EEEE_PartialRefundExceedsOriginalQuantity(
                    productNumber,
                    refundedTotals.Quantity,
                    originalItem.Quantity);
            }

            // Check amount
            if (refundedTotals.Amount > originalItem.Amount + 0.01m)
            {
                var productNumber = originalItem.ProductNumber ?? originalItem.Description ?? key;
                return ErrorMessagesPT.EEEE_PartialRefundExceedsOriginalAmount(
                    productNumber,
                    refundedTotals.Amount,
                    originalItem.Amount);
            }
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Loads all existing refunds for a given receipt reference
    /// </summary>
    private async Task<List<ReceiptRequest>> LoadExistingRefundsAsync(string originalReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        var existingRefunds = new List<ReceiptRequest>();

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
                    request.cbPreviousReceiptReference != null)
                {
                    // Check if this is a partial refund (no full refund flag) that references our original
                    var isFullRefund = request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
                    var previousRef = request.cbPreviousReceiptReference.ToString();
                    
                    // Only include partial refunds (items with refund flag but receipt case without refund flag)
                    if (!isFullRefund && previousRef == originalReceiptReference)
                    {
                        // Check if any items have refund flag
                        if (request.cbChargeItems?.Any(item => item.IsRefund()) == true)
                        {
                            existingRefunds.Add(request);
                        }
                    }
                }
            }
            catch
            {
                // Ignore deserialization errors and continue
                continue;
            }
        }

        return existingRefunds;
    }

    /// <summary>
    /// Builds a dictionary of charge items keyed by a unique identifier
    /// </summary>
    private Dictionary<string, ChargeItem> BuildItemDictionary(IEnumerable<ChargeItem> items)
    {
        var dictionary = new Dictionary<string, ChargeItem>();
        
        foreach (var item in items)
        {
            // Skip discounts/extras as they are not products
            if (item.IsDiscountOrExtra())
            {
                continue;
            }

            var key = GetItemKey(item);
            
            // If we already have this key, aggregate the quantities and amounts
            if (dictionary.ContainsKey(key))
            {
                var existing = dictionary[key];
                dictionary[key] = new ChargeItem
                {
                    ProductNumber = item.ProductNumber,
                    Description = item.Description,
                    Quantity = existing.Quantity + item.Quantity,
                    Amount = existing.Amount + item.Amount,
                    VATRate = item.VATRate,
                    ftChargeItemCase = item.ftChargeItemCase
                };
            }
            else
            {
                dictionary[key] = item;
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Gets a unique key for a charge item based on product number and description
    /// </summary>
    private string GetItemKey(ChargeItem item)
    {
        // Use product number as primary key, fall back to description
        var key = !string.IsNullOrEmpty(item.ProductNumber)
            ? item.ProductNumber
            : item.Description ?? Guid.NewGuid().ToString();
        
        // Include VAT rate in the key to differentiate same products with different VAT
        return $"{key}_{item.VATRate}";
    }
}
