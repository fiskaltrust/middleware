using System;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;

namespace fiskaltrust.Middleware.SCU.GR.MyData.Helpers;

public static class CountryTypeMapper
{
    public static bool TryParseCountryCode(string isoCode, out CountryType country)
    {
        country = default;

        if (string.IsNullOrWhiteSpace(isoCode))
            return false;

        // Normalize to uppercase for case-insensitive match
        isoCode = isoCode.Trim().ToUpperInvariant();

        return Enum.TryParse(isoCode, out country);
    }
}