using System.Text.RegularExpressions;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public static class TaxIdValidation
{
    private static readonly int[] ValidPortugueseFirstDigits = [1, 2, 3, 5, 6, 7, 8, 9];

    public static bool IsValidPortugueseNif(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return false;

        taxId = taxId.Trim();

        if (taxId.Length != 9 || !taxId.All(char.IsDigit))
            return false;

        var digits = taxId.Select(c => int.Parse(c.ToString())).ToArray();

        if (!ValidPortugueseFirstDigits.Contains(digits[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += digits[i] * (9 - i);
        }

        var remainder = sum % 11;
        var expectedCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        return digits[8] == expectedCheckDigit;
    }
}
