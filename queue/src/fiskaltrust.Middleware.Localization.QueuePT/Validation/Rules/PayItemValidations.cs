using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public static class PayItemValidations
{
    /// <summary>
    /// Validates that cash payments do not exceed 3000€.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_PayItems_CashPaymentLimit(ReceiptRequest request)
    {
        if (request.cbPayItems == null || request.cbPayItems.Count == 0)
        {
            yield break;
        }

        var totalCashAmount = request.cbPayItems
            .Where(payItem => payItem.ftPayItemCase.Case() == PayItemCase.CashPayment)
            .Sum(payItem => payItem.Amount);

        if (totalCashAmount > 3000m)
        {
            var rule = PortugalValidationRules.CashPaymentExceedsLimit;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CashPaymentExceedsLimit,
                rule.Code,
                rule.Field
            ).WithContext("TotalCashAmount", totalCashAmount)
             .WithContext("Limit", 3000m));
        }
    }
}
