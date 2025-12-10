using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

public static class SpainValidationHelpers
{
    /// <summary>
    /// Validates a Portuguese Tax Identification Number (NIF - Número de Identificação Fiscal).
    /// Based on the algorithm described at: https://pt.wikipedia.org/wiki/N%C3%BAmero_de_identifica%C3%A7%C3%A3o_fiscal
    /// </summary>
    /// <param name="taxId">The tax ID to validate</param>
    /// <returns>True if the tax ID is valid, false otherwise</returns>
    public static bool IsValidSpanishTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return false;
        }

        // Clean the input: remove spaces and convert to uppercase
        taxId = taxId.Trim().ToUpper();

        // Must be exactly 9 digits
        if (taxId.Length != 9 || !taxId.All(char.IsDigit))
        {
            return false;
        }

        var digits = taxId.Select(c => int.Parse(c.ToString())).ToArray();

        // First digit must be valid (1, 2, 3, 5, 6, 7, 8, or 9)
        // 1 - Pessoa singular (natural person)
        // 2 - Pessoa singular (natural person)
        // 3 - Pessoa singular (natural person)
        // 5 - Pessoa coletiva (legal entity)
        // 6 - Administração pública (public administration)
        // 7 - Herança indivisa (undivided inheritance)
        // 8 - Empresário em nome individual (sole proprietor)
        // 9 - Pessoa coletiva (legal entity)
        var validFirstDigits = new[] { 1, 2, 3, 5, 6, 7, 8, 9 };
        if (!validFirstDigits.Contains(digits[0]))
        {
            return false;
        }

        // Calculate check digit using the Luhn-like algorithm
        // Multiply each of the first 8 digits by (9 - position)
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += digits[i] * (9 - i);
        }

        // Calculate the check digit
        var remainder = sum % 11;
        var expectedCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        // Verify the 9th digit matches the calculated check digit
        return digits[8] == expectedCheckDigit;
    }
}
