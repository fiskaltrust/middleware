namespace fiskaltrust.Middleware.Localization.v2.Validation;

public static class ErrorMessages
{
    public static string EEEE_ChargeItemValidationFailed(int position, string field) =>
        $"EEEE_Charge item at position {position}: {field} validation failed.";
}
