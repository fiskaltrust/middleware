using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Validation.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

/// <summary>
/// Provides comprehensive validation for receipt requests in Portugal.
/// Collects all validation errors (one ValidationResult per error) and can combine them.
/// </summary>
public class ReceiptValidator(ReceiptRequest request, ReceiptResponse receiptResponse, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
{
    private readonly ReceiptRequest _receiptRequest = request;
    readonly ReceiptResponse _receiptResponse = receiptResponse;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
    private readonly RefundValidator _refundValidator = new(readOnlyQueueItemRepository);
    private readonly VoidValidator _voidValidator = new(readOnlyQueueItemRepository);

    /// <summary>
    /// Validates the receipt request and returns all validation errors.
    /// Each validation rule returns one ValidationResult per error found.
    /// </summary>
    public async IAsyncEnumerable<ValidationResult> Validate(ReceiptValidationContext context)
    {
        if (_receiptRequest.ftReceiptCase.Country() != "ES")
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_InvalidCountryCodeForES,
                   "EEEE_InvalidCountryCodeForES",
                   "ftReceiptCase"
               ));
            yield break;
        }
        if (_receiptRequest.cbChargeItems is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_ChargeItemsMissing,
                   "EEEE_ChargeItemsMissing",
                   "cbChargeItems"
               ));
            yield break;
        }
        if (_receiptRequest.cbPayItems is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_PayItemsMissing,
                   "EEEE_PayItemsMissing",
                   "cbPayItems"
               ));
            yield break;
        }
        if (_receiptRequest.cbChargeItems.Any(ci => ci.ftChargeItemCase.Country() != "ES"))
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_InvalidCountryCodeInChargeItemsForES,
                   "EEEE_InvalidCountryCodeInChargeItemsForES",
                   "cbChargeItems"
               ));
            yield break;
        }

        if (_receiptRequest.cbPayItems.Any(ci => ci.ftPayItemCase.Country() != "ES"))
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_InvalidCountryCodeInPayItemsForES,
                   "EEEE_InvalidCountryCodeInPayItemsForES",
                   "cbPayItems"
               ));
            yield break;
        }


        foreach (var result in CustomerValidations.ValidateCustomerTaxId(_receiptRequest))
        {
            yield return result;
        }

        // Run all applicable validations and collect results (one per error)
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_MandatoryFields(_receiptRequest))
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


        if (context.IsRefund)
        {
            foreach (var result in ReceiptRequestValidations.ValidateRefundHasPreviousReference(_receiptRequest))
            {
                yield return result;
            }
        }

        if (_receiptRequest.Currency != Currency.EUR)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_OnlyEuroCurrencySupported,
                   "EEEE_OnlyEuroCurrencySupported",
                   "Currency"
               ));
        }

        if (_receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && _receiptRequest.cbPreviousReceiptReference is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
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


        if (_receiptRequest.cbPreviousReceiptReference is not null)
        {
            if (_receiptRequest.cbPreviousReceiptReference.IsSingle)
            {
                var status = await _receiptReferenceProvider.HasExistingVoidAsync(_receiptRequest.cbPreviousReceiptReference.SingleValue!);
                if (status)
                {
                    yield return ValidationResult.Failed(new ValidationError(
                       ErrorMessagesES.EEEE_HasBeenVoidedAlready(_receiptRequest.cbPreviousReceiptReference.SingleValue!),
                       "EEEE_PreviousReceiptIsVoided",
                       "cbPreviousReceiptReference"
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
                        yield return ValidationResult.Failed(new ValidationError(
                           ErrorMessagesES.EEEE_HasBeenVoidedAlready(reference),
                           "EEEE_PreviousReceiptIsVoided",
                           "cbPreviousReceiptReference"
                       ));
                    }
                }
            }
        }
    }

    private async Task<ValidationResult> ValidateRefundAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            throw new NotSupportedException(ErrorMessagesES.MultipleReceiptReferencesNotSupported);
        }

        return ValidationResult.Success();
    }

    private async Task<ValidationResult> ValidateVoidAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        if (_receiptRequest.cbPreviousReceiptReference is null)
        {
            return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesES.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
        }

        var receiptReferences = receiptResponse.GetRequiredPreviousReceiptReference();
        if (receiptReferences.Count > 1)
        {
            throw new NotSupportedException(ErrorMessagesES.MultipleReceiptReferencesNotSupported);
        }

        var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
        var hasExistingVoid = await _voidValidator.HasExistingVoidAsync(previousReceiptRef);
        if (hasExistingVoid)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesES.EEEE_VoidAlreadyExists(previousReceiptRef));
            return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_VoidAlreadyExists(previousReceiptRef),
                "EEEE_VoidAlreadyExists",
                "cbPreviousReceiptReference"
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

    /// <summary>
    /// Helper method to get all validation results as a list and check if any failed.
    /// </summary>
    public async Task<ValidationResultCollection> ValidateAndCollectAsync(ReceiptValidationContext context) => new ValidationResultCollection(await Validate(context).ToListAsync());
}