using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;

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
        if (request.cbUser == null)
        {
            yield break;
        }

        // Use a list to collect results since we can't yield from try-catch
        var results = new List<ValidationResult>();

        try
        {
            var userJson = System.Text.Json.JsonSerializer.Serialize(request.cbUser);
            var userObject = System.Text.Json.JsonSerializer.Deserialize<Logic.Exports.SAFTPT.SAFTSchemaPT10401.PTUserObject>(userJson);

            if (userObject == null)
            {
                results.Add(ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser could not be deserialized to PTUserObject structure."),
                    "EEEE_InvalidUserStructure",
                    "cbUser"
                )));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userObject.UserId))
                {
                    results.Add(ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser must contain a non-empty 'UserId' property."),
                        "EEEE_InvalidUserStructure",
                        "cbUser.UserId"
                    )));
                }

                if (!string.IsNullOrWhiteSpace(userObject.UserDisplayName) && userObject.UserDisplayName.Length < 3)
                {
                    results.Add(ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser.UserDisplayName must be at least 3 characters long. Current length: {userObject.UserDisplayName.Length}"),
                        "EEEE_InvalidUserStructure",
                        "cbUser.UserDisplayName"
                    ).WithContext("ActualLength", userObject.UserDisplayName.Length)
                     .WithContext("MinimumLength", 3)));
                }
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

    /// <summary>
    /// Validates that cbUser is present (not null or empty string).
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_cbUser_Presence(ReceiptRequest request)
    {
        // Check if cbUser is null or if it's a string that's empty/whitespace
        if (request.cbUser == null ||
            request.cbUser is string userString && string.IsNullOrWhiteSpace(userString))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_UserMissing,
                "EEEE_UserMissing",
                "cbUser"
            ));
        }
    }
}
