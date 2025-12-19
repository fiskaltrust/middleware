using System.Reflection.Emit;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using Newtonsoft.Json.Linq;

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

        if (refundRequest.cbChargeItems.Count != originalRequest.cbChargeItems.Count)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
        }

        if (refundRequest.cbPayItems.Count != originalRequest.cbPayItems.Count)
        {
            return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
        }

        var (flowControl, value) = CompareReceiptRequest(originalReceiptReference, refundRequest, originalRequest);
        if (!flowControl)
        {
            return value;
        }

        for (int i = 0; i < refundRequest.cbChargeItems.Count; i++)
        {
            var refundItem = refundRequest.cbChargeItems[i];
            var originalItem = originalRequest.cbChargeItems[i];

            (flowControl, value) = CompareChargeItems(originalReceiptReference, refundItem, originalItem);
            if (!flowControl)
            {
                return value;
            }
        }
        return null; // Validation passed
    }

    public static (bool flowControl, string? value) CompareReceiptRequest(string originalReceiptReference, ReceiptRequest refundItem, ReceiptRequest originalItem)
    {
        // We ignore cbTerminalID cause it can be different
        // We ignore cbReceiptReference cause it will be different
        // We ignore the cbReceiptMoment because it must be different

        if (originalItem.ftCashBoxID != refundItem.ftCashBoxID)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        // We ignore ftPOSSystemId cause it can be different

        var originalCase = ((long) originalItem.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ftReceiptCaseData != refundItem.ftReceiptCaseData)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        // We ignore cbPreviousReceiptReference because it will be different
        // We ignore cbUser because it will be different

        if (originalItem.cbArea != refundItem.cbArea)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }
        
        if(originalItem.cbCustomer is string originalCustomer && refundItem.cbCustomer is string refundCustomer)
        {
            if (originalCustomer != refundCustomer)
            {
                return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
            }
        }

        if (originalItem.cbCustomer is JsonElement originalJsonCustomer && refundItem.cbCustomer is JsonElement refundJsonCustomer)
        {
            if (!JsonSerializer.Serialize(originalJsonCustomer).Equals(JsonSerializer.Serialize(refundJsonCustomer), StringComparison.Ordinal))
            {
                return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
            }
        }

        if (originalItem.cbSettlement != refundItem.cbSettlement)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        return (flowControl: true, value: null);
    }

    public static (bool flowControl, string? value) CompareChargeItems(string originalReceiptReference, ChargeItem refundItem, ChargeItem originalItem)
    {
        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Description != refundItem.Description)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.VATRate - refundItem.VATRate) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        var originalCase = ((long) originalItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ftChargeItemCaseData != refundItem.ftChargeItemCaseData)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.GetVATAmount() - Math.Abs(refundItem.GetVATAmount())) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        // Moment can be different
        if (originalItem.Position != refundItem.Position)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ProductGroup != refundItem.ProductGroup)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ProductGroup != refundItem.ProductGroup)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ProductNumber != refundItem.ProductNumber)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ProductBarcode != refundItem.ProductBarcode)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Unit != refundItem.Unit)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.UnitQuantity ?? 0.0m - Math.Abs(refundItem.UnitQuantity ?? 0.0m)) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.UnitPrice ?? 0.0m - Math.Abs(refundItem.UnitPrice ?? 0.0m)) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        return (flowControl: true, value: null);
    }

    public static (bool flowControl, string? value) ComparePayItems(string originalReceiptReference, PayItem refundItem, PayItem originalItem)
    {
        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Description != refundItem.Description)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        var originalCase = ((long) originalItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long) refundItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.ftPayItemCaseData != refundItem.ftPayItemCaseData)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        // Moment can be different
        if (originalItem.Position != refundItem.Position)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.MoneyGroup != refundItem.MoneyGroup)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.MoneyNumber != refundItem.MoneyNumber)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.MoneyBarcode != refundItem.MoneyBarcode)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return (flowControl: false, value: ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference));
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
        var (flowControl, value) = CompareReceiptRequest(originalReceiptReference, refundRequest, originalRequest);
        if (!flowControl)
        {
            return value;
        }

        var chargeItemsAvailable = originalRequest.cbChargeItems;
        foreach(var existingRefund in existingRefunds.SelectMany(x => x.cbChargeItems))
        {
            var matchingItem = chargeItemsAvailable.FirstOrDefault(item =>
                (Math.Abs(item.Amount - Math.Abs(existingRefund.Amount)) < 0.01m) &&
                item.Description == existingRefund.Description &&
                (Math.Abs(item.VATRate - existingRefund.VATRate) < 0.01m));
            if(matchingItem != null)
            {
                chargeItemsAvailable.Remove(matchingItem);
            }
        }

        for (int i = 0; i < refundRequest.cbChargeItems.Count; i++)
        {
            var matchingItem = chargeItemsAvailable.FirstOrDefault(item =>
                (Math.Abs(item.Amount - Math.Abs(refundRequest.cbChargeItems[i].Amount)) < 0.01m) &&
                item.Description == refundRequest.cbChargeItems[i].Description &&
                (Math.Abs(item.VATRate - refundRequest.cbChargeItems[i].VATRate) < 0.01m));
            if (matchingItem == null)
            {
                return ErrorMessagesPT.EEEE_FullRefundItemsMismatch(originalReceiptReference);
            }

            (flowControl, value) = CompareChargeItems(originalReceiptReference, refundRequest.cbChargeItems[i], matchingItem);
            if (!flowControl)
            {
                return value;
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
