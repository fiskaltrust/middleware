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
        return true;
    }
}
