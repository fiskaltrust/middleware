using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

/// <summary>
/// Shared validation logic for Portugal receipts.
/// These methods return IEnumerable of ValidationResult objects, with one result per error.
/// </summary>
/// 
public static class cbUserValidations
{
    /// <summary>
    /// Validates that cbUser follows the required PTUserObject structure.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_cbUser_Structure(ReceiptRequest request)
    {
        var results = new List<ValidationResult>();
        try
        {
            var user = request.GetcbUserOrNull();
            if (string.IsNullOrEmpty(user) || user.Length < 3)
            {
                results.Add(ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UserTooShort,
                    "EEEE_UserTooShort",
                    "cbUser"
                )));
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            results.Add(ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser format is invalid: {ex.Message}"),
                "EEEE_InvalidUserStructure",
                "cbUser"
            ).WithContext("ExceptionMessage", ex.Message)));
        }

        foreach (var result in results)
        {
            yield return result;
        }
    }
}
