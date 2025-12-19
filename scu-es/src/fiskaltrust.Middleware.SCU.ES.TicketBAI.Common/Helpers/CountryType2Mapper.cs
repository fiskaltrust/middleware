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
    /// Maps a customer country string to CountryType2 enum.
    /// </summary>
    /// <param name="customerCountry">The customer country string from MiddlewareCustomer</param>
    /// <returns>The mapped CountryType2 enum value</returns>
    /// <exception cref="ArgumentException">Thrown when the country code is invalid or not supported</exception>
    public static CountryType2 MapCustomerCountry(string? customerCountry)
    {
        if (TryParseCountryCode(customerCountry, out var country))
        {
            return country;
        }

        // Provide a clear error message indicating the invalid country code
        var errorMessage = string.IsNullOrWhiteSpace(customerCountry)
            ? "Customer country code is required but was not provided or is empty."
            : $"Invalid or unsupported customer country code: '{customerCountry}'. Expected a valid ISO 3166-1 alpha-2 country code (e.g., 'US', 'DE', 'ES', 'FR').";

        throw new ArgumentException(errorMessage, nameof(customerCountry));
    }
}
