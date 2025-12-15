using System;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

public static class CountryType2Mapper
{
    /// <summary>
    /// Tries to parse an ISO 3166-1 alpha-2 country code string to CountryType2 enum.
    /// </summary>
    /// <param name="isoCode">The ISO country code (e.g., "US", "DE", "FR")</param>
    /// <param name="country">The mapped CountryType2 enum value if successful</param>
    /// <returns>True if the mapping was successful, false otherwise</returns>
    public static bool TryParseCountryCode(string? isoCode, out CountryType2 country)
    {
        country = default;

        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return false;
        }

        // Normalize to uppercase for case-insensitive match
        isoCode = isoCode.Trim().ToUpperInvariant();

        // Reject numeric strings - country codes should be alphabetic
        if (isoCode.All(char.IsDigit))
        {
            return false;
        }

        // Try to parse and verify it's a defined value in the enum
        if (Enum.TryParse(isoCode, out country) && Enum.IsDefined(typeof(CountryType2), country))
        {
            return true;
        }

        country = default;
        return false;
    }

    /// <summary>
    /// Maps a customer country string to CountryType2 enum, with a fallback to US if parsing fails.
    /// </summary>
    /// <param name="customerCountry">The customer country string from MiddlewareCustomer</param>
    /// <returns>The mapped CountryType2 enum value, or US as default</returns>
    public static CountryType2 MapCustomerCountry(string? customerCountry)
    {
        if (TryParseCountryCode(customerCountry, out var country))
        {
            return country;
        }

        // Default to US if parsing fails (matching the current hardcoded value in TicketBaiFactory)
        return CountryType2.US;
    }
}
