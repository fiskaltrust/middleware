using System;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction.Validation;

/// <summary>
/// Validation and normalization of the Italian tax identifiers carried by <see cref="Customer"/>: the
/// codice fiscale (<see cref="Customer.CustomerId"/>) and the partita IVA (<see cref="Customer.CustomerVATId"/>).
/// Algorithms: DM 23/12/1976 (codice fiscale and its check character) and DPR 605/1973 art. 4
/// (partita IVA check digit).
/// </summary>
public static class ItalyValidationHelpers
{
    private const int CodiceFiscaleLength = 16;
    private const int PartitaIvaLength = 11;
    private const string CountryPrefix = "IT";

    /// <summary>Month letters of the codice fiscale; index + 1 is the month.</summary>
    private const string MonthLetters = "ABCDEHLMPRST";

    /// <summary>Letters that replace a digit in case of omocodia; the index is the replaced digit.</summary>
    private const string OmocodiaLetters = "LMNPQRSTUV";

    /// <summary>
    /// Value of a character sitting on an odd (1-based) position of the codice fiscale: digits are first
    /// mapped to letters ('0' -> 'A' ... '9' -> 'J'), the value is then the index in this string.
    /// </summary>
    private const string OddPositionValues = "BAKPLCQDREVOSFTGUHMINJWZYX";

    /// <summary>Trims and upper-cases a tax identifier. Returns an empty string for null.</summary>
    public static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    /// <summary>
    /// <see cref="Normalize(string?)"/> plus unconditional removal of the optional 'IT' country prefix,
    /// which the RT devices do not expect on a partita IVA.
    /// </summary>
    public static string NormalizeVatId(string? value)
    {
        var normalized = Normalize(value);
        return normalized.StartsWith(CountryPrefix, StringComparison.Ordinal)
            ? normalized.Substring(CountryPrefix.Length)
            : normalized;
    }

    /// <summary>
    /// <see cref="Normalize(string?)"/> plus removal of the 'IT' country prefix, but only when what
    /// remains is an 11 digit partita IVA. A codice fiscale is never stripped, because a surname can
    /// legitimately start with 'IT' (e.g. ITALIANO) and blind stripping would corrupt the code.
    /// </summary>
    public static string NormalizeTaxCode(string? value)
    {
        var normalized = Normalize(value);
        if (!normalized.StartsWith(CountryPrefix, StringComparison.Ordinal))
        {
            return normalized;
        }

        var withoutPrefix = normalized.Substring(CountryPrefix.Length);
        return ((withoutPrefix.Length == PartitaIvaLength) && IsAllDigits(withoutPrefix)) ? withoutPrefix : normalized;
    }

    /// <summary>
    /// Picks the tax identifier to hand to a device that offers a single slot for it (the Custom printer
    /// fixed line, the Epson directIO and printRecTaxID): the codice fiscale takes precedence over the
    /// partita IVA. Both values are normalized, so a whitespace only or empty codice fiscale correctly
    /// falls through to the partita IVA. Returns an empty string when the customer carries neither.
    /// </summary>
    public static string SelectCustomerTaxId(string? customerId, string? customerVatId)
    {
        var taxCode = NormalizeTaxCode(customerId);
        return (taxCode.Length > 0) ? taxCode : NormalizeVatId(customerVatId);
    }

    /// <summary>
    /// Validates an Italian partita IVA: 11 digits with a valid check digit (DPR 605/1973 art. 4).
    /// An 'IT' country prefix is accepted and stripped; any other country prefix makes the value invalid.
    /// </summary>
    public static bool IsValidPartitaIva(string? partitaIva)
    {
        if (string.IsNullOrWhiteSpace(partitaIva))
        {
            return false;
        }

        var value = NormalizeVatId(partitaIva);
        if ((value.Length != PartitaIvaLength) || !IsAllDigits(value))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < (PartitaIvaLength - 1); i++)
        {
            var digit = value[i] - '0';

            // Digits on even 1-based positions are doubled; a two digit result is reduced by 9.
            if ((i % 2) != 0)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        var checkDigit = value[PartitaIvaLength - 1] - '0';

        // '00000000000' satisfies the checksum but is never an issued partita IVA - it is a placeholder
        // that PoS systems send when no customer is known, so it is rejected explicitly.
        if ((sum == 0) && (checkDigit == 0))
        {
            return false;
        }

        return checkDigit == ((10 - (sum % 10)) % 10);
    }

    /// <summary>
    /// Validates an Italian codice fiscale: 16 characters matching
    /// ^[A-Z]{6}[0-9LMNPQRSTUV]{2}[ABCDEHLMPRST][0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{3}[A-Z]$ with a
    /// plausible day of birth and a valid check character (CIN).
    /// Legal entities use their 11 digit partita IVA as codice fiscale, so such values are accepted as
    /// well and validated with <see cref="IsValidPartitaIva(string?)"/>.
    /// </summary>
    public static bool IsValidCodiceFiscale(string? codiceFiscale)
    {
        if (string.IsNullOrWhiteSpace(codiceFiscale))
        {
            return false;
        }

        var value = Normalize(codiceFiscale);
        if (value.Length != CodiceFiscaleLength)
        {
            return IsValidPartitaIva(value);
        }

        return HasValidCodiceFiscaleShape(value) && (value[CodiceFiscaleLength - 1] == CalculateCin(value));
    }

    private static bool HasValidCodiceFiscaleShape(string value)
    {
        // Positions 1-6 carry the surname and name letters.
        for (var i = 0; i < 6; i++)
        {
            if (!IsLetter(value[i]))
            {
                return false;
            }
        }

        // Position 12 is the first character of the Belfiore code, position 16 the check character.
        if (!IsLetter(value[11]) || !IsLetter(value[15]))
        {
            return false;
        }

        // Position 9 is the month of birth.
        if (MonthLetters.IndexOf(value[8]) < 0)
        {
            return false;
        }

        // Positions 7-8 (year), 10-11 (day) and 13-15 (Belfiore digits) carry a number, written either as
        // a digit or - in case of omocodia - as a substitute letter.
        if (!IsNumericCharacter(value[6]) || !IsNumericCharacter(value[7])
            || !IsNumericCharacter(value[9]) || !IsNumericCharacter(value[10])
            || !IsNumericCharacter(value[12]) || !IsNumericCharacter(value[13]) || !IsNumericCharacter(value[14]))
        {
            return false;
        }

        // The day of birth is increased by 40 for women.
        var day = (DecodeDigit(value[9]) * 10) + DecodeDigit(value[10]);
        return ((day >= 1) && (day <= 31)) || ((day >= 41) && (day <= 71));
    }

    /// <summary>
    /// Calculates the check character (CIN) over the first 15 characters. The characters are used exactly
    /// as they are written: omocodia letters are deliberately NOT decoded back to digits, because the CIN
    /// of an omocode is recalculated on the substituted code. Only called after
    /// <see cref="HasValidCodiceFiscaleShape(string)"/>, so every character is a digit or A-Z.
    /// </summary>
    private static char CalculateCin(string value)
    {
        var sum = 0;
        for (var i = 0; i < (CodiceFiscaleLength - 1); i++)
        {
            var character = value[i];
            var isDigit = (character >= '0') && (character <= '9');
            if ((i % 2) == 0)
            {
                var letter = isDigit ? (char) ('A' + (character - '0')) : character;
                sum += OddPositionValues.IndexOf(letter);
            }
            else
            {
                sum += isDigit ? (character - '0') : (character - 'A');
            }
        }

        return (char) ('A' + (sum % 26));
    }

    private static bool IsLetter(char character) => (character >= 'A') && (character <= 'Z');

    private static bool IsNumericCharacter(char character) => DecodeDigit(character) >= 0;

    /// <summary>Digit carried by the given character, decoding the omocodia letters, or -1.</summary>
    private static int DecodeDigit(char character) =>
        ((character >= '0') && (character <= '9')) ? (character - '0') : OmocodiaLetters.IndexOf(character);

    private static bool IsAllDigits(string value)
    {
        foreach (var character in value)
        {
            if ((character < '0') || (character > '9'))
            {
                return false;
            }
        }

        return true;
    }
}
