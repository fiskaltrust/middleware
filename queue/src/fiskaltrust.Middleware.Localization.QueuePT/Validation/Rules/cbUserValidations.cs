using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public static class cbUserValidations
{
    public static IEnumerable<ValidationResult> Validate_cbUser_Structure(ReceiptRequest request)
    {
        var results = new List<ValidationResult>();
        try
        {
            var user = request.GetcbUserOrNull();
            if (string.IsNullOrEmpty(user) || user.Length < 3)
            {
                var rule = PortugalValidationRules.UserTooShort;
                results.Add(ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UserTooShort,
                    rule.Code,
                    rule.Field
                )));
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            var rule = PortugalValidationRules.InvalidUserStructure;
            results.Add(ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser format is invalid: {ex.Message}"),
                rule.Code,
                rule.Field
            ).WithContext("ExceptionMessage", ex.Message)));
        }

        foreach (var result in results)
        {
            yield return result;
        }
    }
}
