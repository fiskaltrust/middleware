using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

public static class SpainValidationHelpers
{
    public static bool IsValidSpanishTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return false;
        }

        var regex = new Regex("(([a-z|A-Z]{1}\\d{7}[a-z|A-Z]{1})|(\\d{8}[a-z|A-Z]{1})|([a-z|A-Z]{1}\\d{8}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (!regex.IsMatch(taxId))
        {
            return false;
        }
        return true;
    }
}
