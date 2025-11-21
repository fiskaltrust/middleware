using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation;

/// <summary>
/// Provides comprehensive validation for receipt requests in Portugal.
/// Collects all validation errors (one ValidationResult per error) and can combine them.
/// </summary>
public class ReceiptValidator
{
    private readonly ReceiptRequest _request;

    public ReceiptValidator(ReceiptRequest request)
    {
        _request = request;
    }

    /// <summary>
    /// Validates the receipt request and returns all validation errors.
    /// Each validation rule returns one ValidationResult per error found.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ReceiptValidationContext context)
    {
        foreach (var result in CustomerValidations.ValidateCustomerTaxId(_request))
        {
            yield return result;
        }
        
        // Run all applicable validations and collect results (one per error)
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_MandatoryFields(_request))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_Description_Length(_request))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_SupportedVatRates(_request))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_ftChargeItemCase_SupportedChargeItemCases(_request))
        {
            yield return result;
        }

        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_VatRateAndAmount(_request))
        {
            yield return result;
        }

        // Validate zero VAT rate items have proper exempt reasons
        foreach (var result in ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(_request))
        {
            yield return result;
        }
        
        if (!context.IsRefund)
        {
            foreach (var result in ChargeItemValidations.Validate_ChargeItems_Amount_Quantity_NegativeAmountsAndQuantities(_request, context.IsRefund))
            {
                yield return result;
            }
        }
        
        foreach (var result in ReceiptRequestValidations.ValidateReceiptBalance(_request))
        {
            yield return result;
        }
        
        foreach (var result in cbUserValidations.Validate_cbUser_Structure(_request))
        {
            yield return result;
        }

        foreach (var result in PayItemValidations.Validate_PayItems_CashPaymentLimit(_request))
        {
            yield return result;
        }
        
        if (!context.IsRefund)
        {
            foreach (var result in ChargeItemValidations.Validate_ChargeItems_NetAmountLimit(_request))
            {
                yield return result;
            }

            foreach (var result in ReceiptRequestValidations.ValidateOtherServiceNetAmountLimit(_request))
            {
                yield return result;
            }
        }
        else
        {
            foreach (var result in ReceiptRequestValidations.ValidateRefundHasPreviousReference(_request))
            {
                yield return result;
            }
        }

        // Validate receipt moment order if series is provided
        if (context.NumberSeries != null)
        {
            foreach (var result in ReceiptRequestValidations.ValidateReceiptMomentOrder(_request, context.NumberSeries, context.IsHandwritten))
            {
                yield return result;
            }
        }

        if(_request.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) && _request.cbPreviousReceiptReference is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
        }


        if (_request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) && _request.cbPreviousReceiptReference is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
        }

        if (_request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && _request.cbPreviousReceiptReference is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
        }
    }

    /// <summary>
    /// Helper method to get all validation results as a list and check if any failed.
    /// </summary>
    public ValidationResultCollection ValidateAndCollect(ReceiptValidationContext context)
    {
        return new ValidationResultCollection(Validate(context).ToList());
    }
}