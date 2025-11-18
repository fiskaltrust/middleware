using fiskaltrust.ifPOS.v2;
using System.Collections.Generic;
using System.Linq;

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
        // Run all applicable validations and collect results (one per error)
        foreach (var result in PortugalValidationRules.ValidateSupportedVatRates(_request))
        {
            yield return result;
        }

        foreach (var result in PortugalValidationRules.ValidateSupportedChargeItemCases(_request))
        {
            yield return result;
        }

        foreach (var result in PortugalValidationRules.ValidateVatRateAndAmount(_request))
        {
            yield return result;
        }
        
        if (!context.IsRefund)
        {
            foreach (var result in PortugalValidationRules.ValidateNegativeAmountsAndQuantities(_request, context.IsRefund))
            {
                yield return result;
            }
        }
        
        foreach (var result in PortugalValidationRules.ValidateReceiptBalance(_request))
        {
            yield return result;
        }
        
        if (context.GeneratesSignature)
        {
            foreach (var result in PortugalValidationRules.ValidateUserPresenceForSignatures(_request, context.GeneratesSignature))
            {
                yield return result;
            }
        }
        
        foreach (var result in PortugalValidationRules.ValidateUserStructure(_request))
        {
            yield return result;
        }

        foreach (var result in PortugalValidationRules.ValidateCashPaymentLimit(_request))
        {
            yield return result;
        }
        
        if (!context.IsRefund)
        {
            foreach (var result in PortugalValidationRules.ValidatePosReceiptNetAmountLimit(_request))
            {
                yield return result;
            }

            foreach (var result in PortugalValidationRules.ValidateOtherServiceNetAmountLimit(_request))
            {
                yield return result;
            }
        }
        else
        {
            foreach (var result in PortugalValidationRules.ValidateRefundHasPreviousReference(_request))
            {
                yield return result;
            }
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

/// <summary>
/// Context information for receipt validation
/// </summary>
public class ReceiptValidationContext
{
    /// <summary>
    /// Whether this is a refund receipt
    /// </summary>
    public bool IsRefund { get; set; }

    /// <summary>
    /// Whether this receipt type generates a signature
    /// </summary>
    public bool GeneratesSignature { get; set; }

    /// <summary>
    /// Whether this is a handwritten receipt
    /// </summary>
    public bool IsHandwritten { get; set; }
}

/// <summary>
/// Collection of validation results with helper methods
/// </summary>
public class ValidationResultCollection
{
    private readonly List<ValidationResult> _results;

    public ValidationResultCollection(List<ValidationResult> results)
    {
        _results = results;
    }

    /// <summary>
    /// Gets all validation results (one per error)
    /// </summary>
    public IReadOnlyList<ValidationResult> Results => _results;

    /// <summary>
    /// Returns true if all validations passed (no errors)
    /// </summary>
    public bool IsValid => !_results.Any(r => !r.IsValid);

    /// <summary>
    /// Gets all errors from all validation results
    /// </summary>
    public IEnumerable<ValidationError> AllErrors => _results.SelectMany(r => r.Errors);

    /// <summary>
    /// Gets combined error message from all results
    /// </summary>
    public string GetCombinedErrorMessage(string separator = " | ")
    {
        return string.Join(separator, _results.SelectMany(r => r.Errors).Select(e => e.Message));
    }

    /// <summary>
    /// Gets all unique error codes
    /// </summary>
    public IEnumerable<string> GetAllErrorCodes()
    {
        return _results
            .SelectMany(r => r.Errors)
            .Where(e => !string.IsNullOrEmpty(e.Code))
            .Select(e => e.Code!)
            .Distinct();
    }
}
