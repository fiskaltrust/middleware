using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;

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
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, "Mismatch ChargeItems");
        }

        if (refundRequest.cbChargeItems.Count != originalRequest.cbChargeItems.Count)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, "Mismatch ChargeItems Count");
        }

        if (refundRequest.cbPayItems == null || originalRequest.cbPayItems == null)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, "Mismatch PayItems");
        }

        if (refundRequest.cbPayItems.Count != originalRequest.cbPayItems.Count)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, "Mismatch PayItems Count");
        }

        var (flowControl, value) = CompareReceiptRequest(originalReceiptReference, refundRequest, originalRequest, isPartial: false);
        if (!flowControl)
        {
            return value;
        }

        for (int i = 0; i < refundRequest.cbChargeItems.Count; i++)
        {
            var refundItem = refundRequest.cbChargeItems[i];
            var originalItem = originalRequest.cbChargeItems[i];

            (flowControl, value) = CompareChargeItems(originalReceiptReference, refundItem, originalItem, isPartial: false);
            if (!flowControl)
            {
                return value;
            }
        }
        return null; // Validation passed
    }

    public static (bool flowControl, string? value) CompareReceiptRequest(string originalReceiptReference, ReceiptRequest refundItem, ReceiptRequest originalItem, bool isPartial = false)
    {
        string Mismatch(string field) => isPartial
            ? ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, field)
            : ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, field);
        // We ignore cbTerminalID cause it can be different
        // We ignore cbReceiptReference cause it will be different
        // We ignore the cbReceiptMoment because it must be different

        if (originalItem.ftCashBoxID != refundItem.ftCashBoxID)
        {
            return (flowControl: false, value: Mismatch("CashBoxID"));
        }

        // We ignore ftPOSSystemId cause it can be different

        var originalCase = ((long) originalItem.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: Mismatch("ReceiptCase"));
        }

        if (originalItem.ftReceiptCaseData != refundItem.ftReceiptCaseData)
        {
            return (flowControl: false, value: Mismatch("ReceiptCaseData"));
        }

        // We ignore cbPreviousReceiptReference because it will be different
        // We ignore cbUser because it will be different

        if (originalItem.cbArea != refundItem.cbArea)
        {
            return (flowControl: false, value: Mismatch("cbArea"));
        }

        var (customersMatch, customerDiff) = CustomersMatch(originalItem.GetCustomerOrNull(), refundItem.GetCustomerOrNull());
        if (!customersMatch)
        {
            var details = string.IsNullOrEmpty(customerDiff) ? string.Empty : $" Different fields: {customerDiff}";
            return (flowControl: false, value: $"{Mismatch("cbCustomer")}.{details}");
        }

        if (originalItem.cbSettlement != refundItem.cbSettlement)
        {
            return (flowControl: false, value: Mismatch("cbSettlement"));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: Mismatch("Currency"));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: Mismatch("DecimalPrecisionMultiplier"));
        }

        return (flowControl: true, value: null);
    }

    private static (bool matches, string? differences) CustomersMatch(MiddlewareCustomer? originalCustomer, MiddlewareCustomer? refundCustomer)
    {
        if (originalCustomer == null && refundCustomer == null)
        {
            return (true, null);
        }

        if (originalCustomer == null || refundCustomer == null)
        {
            return (false, "cbCustomer is null on one side");
        }

        var differences = new List<string>();

        void Compare(string? a, string? b, string field)
        {
            if (!string.Equals(a, b, StringComparison.Ordinal))
            {
                differences.Add(field);
            }
        }

        Compare(originalCustomer.CustomerName, refundCustomer.CustomerName, nameof(MiddlewareCustomer.CustomerName));
        Compare(originalCustomer.CustomerId, refundCustomer.CustomerId, nameof(MiddlewareCustomer.CustomerId));
        Compare(originalCustomer.CustomerType, refundCustomer.CustomerType, nameof(MiddlewareCustomer.CustomerType));
        Compare(originalCustomer.CustomerStreet, refundCustomer.CustomerStreet, nameof(MiddlewareCustomer.CustomerStreet));
        Compare(originalCustomer.CustomerZip, refundCustomer.CustomerZip, nameof(MiddlewareCustomer.CustomerZip));
        Compare(originalCustomer.CustomerCity, refundCustomer.CustomerCity, nameof(MiddlewareCustomer.CustomerCity));
        Compare(originalCustomer.CustomerCountry, refundCustomer.CustomerCountry, nameof(MiddlewareCustomer.CustomerCountry));
        Compare(originalCustomer.CustomerVATId, refundCustomer.CustomerVATId, nameof(MiddlewareCustomer.CustomerVATId));

        return (differences.Count == 0, differences.Count == 0 ? null : string.Join(", ", differences));
    }

    public static (bool flowControl, string? value) CompareChargeItems(string originalReceiptReference, ChargeItem refundItem, ChargeItem originalItem, bool isPartial = false)
    {
        string Mismatch(string field) => isPartial
            ? ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, field)
            : ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, field);

        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("Quantity"));
        }

        if (originalItem.Description != refundItem.Description)
        {
            return (flowControl: false, value: Mismatch("Description"));
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return (flowControl: false, value: Mismatch("Amount"));
        }

        if (Math.Abs(originalItem.VATRate - refundItem.VATRate) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("VATRate"));
        }

        var originalCase = ((long) originalItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: Mismatch("ReceiptCase"));
        }

        if (originalItem.ftChargeItemCaseData != refundItem.ftChargeItemCaseData)
        {
            return (flowControl: false, value: Mismatch("cbCustomer"));
        }

        if (Math.Abs(originalItem.GetVATAmount() - Math.Abs(refundItem.GetVATAmount())) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("VATAmount"));
        }

        // Moment can be different
        if (originalItem.Position != refundItem.Position)
        {
            return (flowControl: false, value: Mismatch("Position"));
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return (flowControl: false, value: Mismatch("AccountNumber"));
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return (flowControl: false, value: Mismatch("CostCenter"));
        }

        if (originalItem.ProductGroup != refundItem.ProductGroup)
        {
            return (flowControl: false, value: Mismatch("ProductGroup"));
        }

        if (originalItem.ProductGroup != refundItem.ProductGroup)
        {
            return (flowControl: false, value: Mismatch("ProductGroup"));
        }

        if (originalItem.ProductNumber != refundItem.ProductNumber)
        {
            return (flowControl: false, value: Mismatch("ProductNumber"));
        }

        if (originalItem.ProductBarcode != refundItem.ProductBarcode)
        {
            return (flowControl: false, value: Mismatch("ProductBarcode"));
        }

        if (originalItem.Unit != refundItem.Unit)
        {
            return (flowControl: false, value: Mismatch("Unit"));
        }

        if (Math.Abs(originalItem.UnitQuantity ?? 0.0m - Math.Abs(refundItem.UnitQuantity ?? 0.0m)) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("UnitQuantity"));
        }

        if (Math.Abs(originalItem.UnitPrice ?? 0.0m - Math.Abs(refundItem.UnitPrice ?? 0.0m)) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("UnitPrice"));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: Mismatch("Currency"));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: Mismatch("DecimalPrecisionMultiplier"));
        }

        return (flowControl: true, value: null);
    }

    public static (bool flowControl, string? value) ComparePayItems(string originalReceiptReference, PayItem refundItem, PayItem originalItem, bool isPartial = false)
    {
        string Mismatch(string field) => isPartial
            ? ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, field)
            : ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference, field);

        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return (flowControl: false, value: Mismatch("Quantity"));
        }

        if (originalItem.Description != refundItem.Description)
        {
            return (flowControl: false, value: Mismatch("Description"));
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return (flowControl: false, value: Mismatch("Amount"));
        }

        var originalCase = ((long) originalItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: Mismatch("ftPayItemCase"));
        }

        if (originalItem.ftPayItemCaseData != refundItem.ftPayItemCaseData)
        {
            return (flowControl: false, value: Mismatch("ftPayItemCaseData"));
        }

        // Moment can be different
        if (originalItem.Position != refundItem.Position)
        {
            return (flowControl: false, value: Mismatch("Position"));
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return (flowControl: false, value: Mismatch("AccountNumber"));
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return (flowControl: false, value: Mismatch("CostCenter"));
        }

        if (originalItem.MoneyGroup != refundItem.MoneyGroup)
        {
            return (flowControl: false, value: Mismatch("MoneyGroup"));
        }

        if (originalItem.MoneyNumber != refundItem.MoneyNumber)
        {
            return (flowControl: false, value: Mismatch("MoneyNumber"));
        }

        if (originalItem.MoneyBarcode != refundItem.MoneyBarcode)
        {
            return (flowControl: false, value: Mismatch("MoneyBarcode"));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: Mismatch("Currency"));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: Mismatch("DecimalPrecisionMultiplier"));
        }

        return (flowControl: true, value: null);
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
        var existingRefunds = await LoadExistingRefundsAsync(refundRequest);
        var (flowControl, value) = CompareReceiptRequest(originalReceiptReference, refundRequest, originalRequest, isPartial: true);
        if (!flowControl)
        {
            return value;
        }

        foreach(var refundItem in refundRequest.cbChargeItems)
        {
            var refundItemIdentifier = SaftExporter.GenerateUniqueProductIdentifier(refundItem);
            var existingRefundItems = existingRefunds.SelectMany(x => x.cbChargeItems).Where(x => SaftExporter.GenerateUniqueProductIdentifier(x) == refundItemIdentifier);
            var originalItems = originalRequest.cbChargeItems.Where(x => SaftExporter.GenerateUniqueProductIdentifier(x) == refundItemIdentifier).ToList();

            if (originalItems.Count == 0)
            {
                return ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, $"No matching item found for product identifier '{refundItem.Description?.Trim()}'");
            }

            var originalTotalQuantity = originalItems.Sum(x => x.Quantity);
            var existingRefundedQuantity = existingRefundItems.Sum(x => x.Quantity);
            if (refundItem.Quantity + existingRefundedQuantity > originalTotalQuantity + 0.001m)
            {
                return ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, "Quantity Exceeded");
            }

            var originalTotalAmount = originalItems.Sum(x => x.Amount);
            var existingRefundedAmount = existingRefundItems.Sum(x => x.Amount);
            if (refundItem.Amount + existingRefundedAmount > originalTotalAmount + 0.01m)
            {
                return ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, "Amount Exceeded");
            }

            var referenceItem = originalItems.First();
            if (Math.Abs(referenceItem.VATRate - refundItem.VATRate) > 0.001m)
            {
                return ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, "VATRate");
            }

            var originalCase = ((long) referenceItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
            var refundCase = ((long) refundItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
            if (originalCase != refundCase)
            {
                return ErrorMessagesPT.EEEE_PartialRefundItemsMismatch(originalReceiptReference, "ChargeItemCase");
            }
        }
        return null; // Validation passed
    }

    /// <summary>
    /// Loads all existing refunds for a given receipt reference
    /// </summary>
    private async Task<List<ReceiptRequest>> LoadExistingRefundsAsync(ReceiptRequest refundRequest)
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
                if (request != null && request.cbPreviousReceiptReference != null && request.cbReceiptReference != refundRequest.cbReceiptReference)
                {
                    var isFullRefund = request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
                    var previousRef = request.cbPreviousReceiptReference;
                    if (request.IsPartialRefundReceipt() && previousRef.SingleValue == refundRequest.cbPreviousReceiptReference.SingleValue)
                    {
                        existingRefunds.Add(request);
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return existingRefunds;
    }
}
