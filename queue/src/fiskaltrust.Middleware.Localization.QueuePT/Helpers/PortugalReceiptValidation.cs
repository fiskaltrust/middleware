using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

/// <summary>
/// Legacy validation helper for Portugal receipts.
/// This class provides individual validation methods for backward compatibility.
/// For new code, consider using ReceiptValidator which returns IEnumerable of ValidationResult (one per error).
/// </summary>
public static class PortugalReceiptValidation
{
    /// <summary>
    /// Validates that cash payments do not exceed 3000€
    /// </summary>
    public static string? ValidateCashPaymentLimit(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateCashPaymentLimit(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that POS receipt net amount does not exceed 1000€
    /// </summary>
    public static string? ValidatePosReceiptNetAmountLimit(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidatePosReceiptNetAmountLimit(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that OtherService charge items do not exceed 100€ net amount
    /// </summary>
    public static string? ValidateOtherServiceNetAmountLimit(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateOtherServiceNetAmountLimit(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that only supported VAT rates are used in charge items.
    /// </summary>
    public static string? ValidateSupportedVatRates(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateSupportedVatRates(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that the VAT rate category matches the specified VAT rate percentage,
    /// and that the VAT amount is correctly calculated.
    /// </summary>
    public static string? ValidateVatRateAndAmount(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateVatRateAndAmount(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that non-refund receipts do not have negative quantities or amounts.
    /// </summary>
    public static string? ValidateNegativeAmountsAndQuantities(ReceiptRequest request, bool isRefund)
    {
        var results = PortugalValidationRules.ValidateNegativeAmountsAndQuantities(request, isRefund).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// </summary>
    public static string? ValidateReceiptBalance(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateReceiptBalance(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that cbUser follows the required PTUserObject structure.
    /// </summary>
    public static string? ValidateUserStructure(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that cbUser is present for receipts that generate signatures.
    /// </summary>
    public static string? ValidateUserPresenceForSignatures(ReceiptRequest request, bool generatesSignature)
    {
        var results = PortugalValidationRules.ValidateUserPresenceForSignatures(request, generatesSignature).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }

    /// <summary>
    /// Validates that only supported charge item cases are used.
    /// </summary>
    public static string? ValidateSupportedChargeItemCases(ReceiptRequest request)
    {
        var results = PortugalValidationRules.ValidateSupportedChargeItemCases(request).ToList();
        return results.Any() ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) : null;
    }
}
