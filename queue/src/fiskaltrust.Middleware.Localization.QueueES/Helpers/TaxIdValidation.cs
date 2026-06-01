using System.Text.RegularExpressions;

namespace fiskaltrust.Middleware.Localization.QueueES.Helpers;

public static class TaxIdValidation
{
    private static readonly Regex SpanishNifRegex = new(
        @"^([a-zA-Z]\d{7}[a-zA-Z]|\d{8}[a-zA-Z]|[a-zA-Z]\d{8})$",
        RegexOptions.Compiled);


    public static bool IsValidSpanishNif(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return false;

        return SpanishNifRegex.IsMatch(taxId.Trim());
    }
}
