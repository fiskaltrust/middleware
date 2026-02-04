using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using Currency = fiskaltrust.ifPOS.v2.Currency;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation;

/// <summary>
/// Provides comprehensive validation for receipt requests in Portugal.
/// Collects all validation errors (one ValidationResult per error) and can combine them.
/// </summary>
public class ReceiptValidator(ReceiptRequest request, ReceiptResponse receiptResponse, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
{
    private readonly ReceiptRequest _receiptRequest = request;
    readonly ReceiptResponse _receiptResponse = receiptResponse;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
    private readonly DocumentStatusProvider _documentStatusProvider = new(readOnlyQueueItemRepository);
    private readonly RefundValidator _refundValidator = new(readOnlyQueueItemRepository);
    private readonly VoidValidator _voidValidator = new(readOnlyQueueItemRepository);

    /// <summary>
    /// Validates the receipt request and returns all validation errors.
    /// Each validation rule returns one ValidationResult per error found.
    /// </summary>
    public async IAsyncEnumerable<ValidationResult> Validate(ReceiptValidationContext context)
    {
        // Validate time difference between cbReceiptMoment and ftReceiptMoment
        foreach (var result in ReceiptRequestValidations.ValidateReceiptMomentTimeDifference(_receiptRequest, _receiptResponse))
        {
            yield return result;
        }


        if (_receiptRequest.ftReceiptCase.Country() != "PT")
        {
            var rule = PortugalValidationRules.InvalidCountryCodeForPT;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_InvalidCountryCodeForPT,
                   rule.Code,
                   rule.Field
               ));
            yield break;
        }
        if (_receiptRequest.cbChargeItems is null)
        {
            var rule = PortugalValidationRules.ChargeItemsMissing;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_ChargeItemsMissing,
                   rule.Code,
                   rule.Field
               ));
            yield break;
        }
        if (_receiptRequest.cbPayItems is null)
        {
            var rule = PortugalValidationRules.PayItemsMissing;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PayItemsMissing,
                   rule.Code,
                   rule.Field
               ));
            yield break;
        }
        if (_receiptRequest.cbChargeItems.Any(ci => ci.ftChargeItemCase.Country() != "PT"))
        {
            var rule = PortugalValidationRules.InvalidCountryCodeInChargeItemsForPT;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_InvalidCountryCodeInChargeItemsForPT,
                   rule.Code,
                   rule.Field
               ));
            yield break;
        }

        if (_receiptRequest.cbPayItems.Any(ci => ci.ftPayItemCase.Country() != "PT"))
        {
            var rule = PortugalValidationRules.InvalidCountryCodeInPayItemsForPT;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_InvalidCountryCodeInPayItemsForPT,
                   rule.Code,
                   rule.Field
               ));
            yield break;
        }

        foreach (var result in ReceiptRequestValidations.ValidatePositions(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in cbCustomerValidations.ValidateCustomerTaxId(_receiptRequest))
        {
            yield return result;
        }

        // Run all applicable validations and collect results (one per error)
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_MandatoryFields(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_Description_Length(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_Quantity_NotZero(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_SupportedVatRates(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_ftChargeItemCase_SupportedChargeItemCases(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_VatRateAndAmount(_receiptRequest))
        {
            yield return result;
        }

        // Validate zero VAT rate items have proper exempt reasons
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(_receiptRequest))
        {
            yield return result;
        }

        // Validate that discounts do not exceed article amounts
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_DiscountExceedsArticleAmount(_receiptRequest))
        {
            yield return result;
        }

        // Validate that discounts/extras are never positive (PT rule)
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_DiscountOrExtra_NotPositive(_receiptRequest))
        {
            yield return result;
        }

        if (!context.IsRefund)
        {
            foreach (var result in ChargeItemValidations.Validate_ChargeItems_Amount_Quantity_NegativeAmountsAndQuantities(_receiptRequest, context.IsRefund))
            {
                yield return result;
            }
        }

        foreach (var result in ReceiptRequestValidations.ValidateReceiptBalance(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in cbUserValidations.Validate_cbUser_Structure(_receiptRequest))
        {
            yield return result;
        }

        foreach (var result in PayItemValidations.Validate_PayItems_CashPaymentLimit(_receiptRequest))
        {
            yield return result;
        }

        if (!context.IsRefund)
        {
            foreach (var result in ChargeItemValidations.Validate_ChargeItems_NetAmountLimit(_receiptRequest))
            {
                yield return result;
            }

            foreach (var result in ReceiptRequestValidations.ValidateOtherServiceNetAmountLimit(_receiptRequest))
            {
                yield return result;
            }
        }
        else
        {
            foreach (var result in ReceiptRequestValidations.ValidateRefundHasPreviousReference(_receiptRequest))
            {
                yield return result;
            }
        }

        // Validate receipt moment order if series is provided
        if (context.NumberSeries != null)
        {
            foreach (var result in ReceiptRequestValidations.ValidateReceiptMomentOrder(_receiptRequest, context.NumberSeries, context.IsHandwritten))
            {
                yield return result;
            }
        }

        if (_receiptRequest.Currency != Currency.EUR)
        {
            var rule = PortugalValidationRules.OnlyEuroCurrencySupported;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_OnlyEuroCurrencySupported,
                   rule.Code,
                   rule.Field
               ));
        }

        if (_receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && _receiptRequest.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.PreviousReceiptReference;
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   rule.Code,
                   rule.Field
               ));
        }

        if (_receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            yield return await ValidateRefundAsync(_receiptRequest, _receiptResponse);
        }

        if (_receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            yield return await ValidateVoidAsync(_receiptRequest, _receiptResponse);
        }

        if (_receiptRequest.IsPartialRefundReceipt())
        {
            yield return await ValidatePartialRefundAsync(_receiptRequest, _receiptResponse);
        }

        if (ShouldValidatePreviousReceiptLineItems(_receiptRequest))
        {
            var receiptReferences = _receiptResponse.GetPreviousReceiptReference();
            var hasConnectableItem = receiptReferences != null &&
                receiptReferences.Count > 0 &&
                receiptReferences.Any(reference => HasConnectableChargeItem(_receiptRequest, reference.Request));

            if (!hasConnectableItem)
            {
                var rule = PortugalValidationRules.PreviousReceiptLineItemMismatch;
                yield return ValidationResult.Failed(new ValidationError(
                       ErrorMessagesPT.EEEE_PreviousReceiptLineItemMismatch,
                       rule.Code,
                       rule.Field
                   ));
            }
        }

        if (_receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            if (_receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) || _receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) || _receiptRequest.IsPartialRefundReceipt())
            {
                var rule = PortugalValidationRules.HandwrittenReceiptsNotSupported;
                yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_HandwrittenReceiptsNotSupported,
                   rule.Code,
                   rule.Field
               ));
            }
        }

        if (_receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            if (!_receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data) || data.PT is null || data.PT.Series is null || !data.PT.Number.HasValue)
            {
                var rule = PortugalValidationRules.HandwrittenReceiptSeriesAndNumberMandatory;
                yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_HandwrittenReceiptSeriesAndNumberMandatory,
                   rule.Code,
                   rule.Field
               ));
            }
        }

        if (_receiptRequest.cbPreviousReceiptReference is not null)
        {
            if (_receiptRequest.cbPreviousReceiptReference.IsSingle)
            {
                var status = await _receiptReferenceProvider.HasExistingVoidAsync(_receiptRequest.cbPreviousReceiptReference.SingleValue!);
                if (status)
                {
                    var rule = PortugalValidationRules.PreviousReceiptIsVoided;
                    yield return ValidationResult.Failed(new ValidationError(
                       ErrorMessagesPT.EEEE_HasBeenVoidedAlready(_receiptRequest.cbPreviousReceiptReference.SingleValue!),
                       rule.Code,
                       rule.Field
                   ));
                }
            }
            else
            {
                foreach (var reference in _receiptRequest.cbPreviousReceiptReference.GroupValue)
                {
                    var status = await _receiptReferenceProvider.HasExistingVoidAsync(reference);
                    if (status)
                    {
                        var rule = PortugalValidationRules.PreviousReceiptIsVoided;
                        yield return ValidationResult.Failed(new ValidationError(
                           ErrorMessagesPT.EEEE_HasBeenVoidedAlready(reference),
                           rule.Code,
                           rule.Field
                       ));
                    }
                }
            }
        }

        if (_receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
        {
            yield return await ValidatePaymentTransferForInvoiceAsync(_receiptRequest, _receiptResponse);
        }
    }

    private async Task<ValidationResult> ValidatePaymentTransferForInvoiceAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.PreviousReceiptReference;
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   rule.Code,
                   rule.Field
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
        }


        if (_receiptRequest.cbChargeItems == null || !_receiptRequest.cbChargeItems.Any(ci => ci.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable)))
        {
            var rule = PortugalValidationRules.PaymentTransferRequiresAccountReceivableItem;
            return ValidationResult.Failed(new ValidationError(
               ErrorMessagesPT.EEEE_PaymentTransferRequiresAccountReceivableItem,
               rule.Code,
               rule.Field
           ));
        }

        var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
        var hasExistingRefund = await _receiptReferenceProvider.HasExistingPaymentTransferAsync(previousReceiptRef);
        if (hasExistingRefund)
        {
            var rule = PortugalValidationRules.RefundAlreadyExists;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_RefundAlreadyExists(previousReceiptRef),
                rule.Code,
                rule.Field
            ));
        }

        // Validate full refund: check if all articles from original invoice are properly refunded
        var originalRequest = receiptReferences[0].Request;
        var validationError = await ValidatePaymentTransferAsync(
            receiptRequest,
            originalRequest,
            previousReceiptRef);

        if (validationError != null)
        {
            return ValidationResult.Failed(validationError);
        }
        else
        {
            return ValidationResult.Success();
        }
    }

    public async Task<string?> ValidatePaymentTransferAsync(
     ReceiptRequest refundRequest,
     ReceiptRequest originalRequest,
     string originalReceiptReference)
    {
        if (originalRequest.ftReceiptCase.Case() != ReceiptCase.InvoiceUnknown0x1000 &&
           originalRequest.ftReceiptCase.Case() != ReceiptCase.InvoiceB2C0x1001 &&
           originalRequest.ftReceiptCase.Case() != ReceiptCase.InvoiceB2B0x1002 &&
           originalRequest.ftReceiptCase.Case() != ReceiptCase.InvoiceB2G0x1003)
        {
            return $"The original receipt '{originalReceiptReference}' is not a valid receipt for payment transfer. Only Invoices are allowed.";
        }

        if (originalRequest.cbPayItems.Where(x => x.ftPayItemCase.Case() == PayItemCase.AccountsReceivable).Sum(x => x.Amount) != refundRequest.cbPayItems.Sum(x => x.Amount))
        {
            return $"The total amount of pay items in the payment transfer receipt must match the total amount of pay items in the original invoice receipt '{originalReceiptReference}'.";
        }
        return null; // Validation passed
    }

    private async Task<ValidationResult> ValidateRefundAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.PreviousReceiptReference;
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   rule.Code,
                   rule.Field
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
        }

        var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
        var hasExistingRefund = await _receiptReferenceProvider.HasExistingRefundAsync(previousReceiptRef);
        if (hasExistingRefund)
        {
            var rule = PortugalValidationRules.RefundAlreadyExists;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_RefundAlreadyExists(previousReceiptRef),
                rule.Code,
                rule.Field
            ));
        }

        // Validate full refund: check if all articles from original invoice are properly refunded
        var originalRequest = receiptReferences[0].Request;
        var validationError = await _refundValidator.ValidateFullRefundAsync(
            receiptRequest,
            originalRequest,
            previousReceiptRef);

        if (validationError != null)
        {
            return ValidationResult.Failed(validationError);
        }
        else
        {
            return ValidationResult.Success();
        }
    }

    private async Task<ValidationResult> ValidateVoidAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.PreviousReceiptReference;
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   rule.Code,
                   rule.Field
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
        }

        var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
        var documentStatus = await _documentStatusProvider.GetDocumentStatusStateAsync((receiptRequest, receiptResponse));
        if (documentStatus.Status == DocumentStatus.Voided)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_VoidAlreadyExists(previousReceiptRef));
            var rule = PortugalValidationRules.VoidAlreadyExists;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_VoidAlreadyExists(previousReceiptRef),
                rule.Code,
                rule.Field
            ));
        }

        if (documentStatus.Status == DocumentStatus.Invoiced)
        {
            var rule = PortugalValidationRules.CannotVoidInvoicedDocument;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CannotVoidInvoicedDocument(previousReceiptRef),
                rule.Code,
                rule.Field
            ));           
        }


        var originalRequest = receiptReferences[0].Request;
        var validationError = await _voidValidator.ValidateVoidAsync(
            receiptRequest,
            originalRequest,
            previousReceiptRef);

        if (validationError != null)
        {
            return ValidationResult.Failed(validationError);
        }
        else
        {
            return ValidationResult.Success();
        }
    }

    private async Task<ValidationResult> ValidatePartialRefundAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            var rule = PortugalValidationRules.PreviousReceiptReference;
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   rule.Code,
                   rule.Field
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            return ValidationResult.Failed(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
        }

        if (receiptRequest.cbChargeItems?.Any(item => !item.IsRefund()) == true)
        {
            var rule = PortugalValidationRules.MixedRefundItemsNotAllowed;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed,
                rule.Code,
                rule.Field
            ));
        }

        if (receiptRequest.cbPayItems?.Any(item => !item.IsRefund()) == true)
        {
            var rule = PortugalValidationRules.MixedRefundPayItemsNotAllowed;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_MixedRefundPayItemsNotAllowed,
                rule.Code,
                rule.Field
            ));
        }

        var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
        var originalRequest = receiptReferences[0].Request;

        var hasExistingRefund = await _receiptReferenceProvider.HasExistingRefundAsync(previousReceiptRef);
        if (hasExistingRefund)
        {
            var rule = PortugalValidationRules.RefundAlreadyExists;
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_RefundAlreadyExists(previousReceiptRef),
                rule.Code,
                rule.Field
            ));
        }

        // Validate partial refund: check for mixed items and quantity/amount limits
        var validationError = await _refundValidator.ValidatePartialRefundAsync(
            receiptRequest,
            originalRequest,
            previousReceiptRef);

        if (validationError != null)
        {
            return ValidationResult.Failed(validationError);
        }
        else
        {
            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Helper method to get all validation results as a list and check if any failed.
    /// </summary>
    public async Task<ValidationResultCollection> ValidateAndCollectAsync(ReceiptValidationContext context) => new ValidationResultCollection(await Validate(context).ToListAsync());

    private static bool ShouldValidatePreviousReceiptLineItems(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbPreviousReceiptReference is null)
        {
            return false;
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ||
            receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) ||
            receiptRequest.IsPartialRefundReceipt())
        {
            return false;
        }

        return true;
    }

    private static bool HasConnectableChargeItem(ReceiptRequest currentRequest, ReceiptRequest originalRequest)
    {
        if (currentRequest.cbChargeItems is null || currentRequest.cbChargeItems.Count == 0 ||
            originalRequest.cbChargeItems is null || originalRequest.cbChargeItems.Count == 0)
        {
            return false;
        }

        var currentIdentifiers = currentRequest.cbChargeItems
            .Where(IsProductChargeItem)
            .Select(SaftExporter.GenerateUniqueProductIdentifier)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.Ordinal);

        if (currentIdentifiers.Count == 0)
        {
            return false;
        }

        foreach (var originalItem in originalRequest.cbChargeItems.Where(IsProductChargeItem))
        {
            var originalIdentifier = SaftExporter.GenerateUniqueProductIdentifier(originalItem);
            if (!string.IsNullOrEmpty(originalIdentifier) && currentIdentifiers.Contains(originalIdentifier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProductChargeItem(ChargeItem chargeItem)
    {
        if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) ||
            chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable))
        {
            return false;
        }

        return true;
    }
}
