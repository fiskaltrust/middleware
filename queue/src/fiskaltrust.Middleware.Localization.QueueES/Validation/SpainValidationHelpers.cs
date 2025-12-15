using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

public static class SpainValidationHelpers
{
    public static bool IsValidSpanishTaxId(MiddlewareCustomer middlewareCustomer)
    {
        if (middlewareCustomer == null || string.IsNullOrWhiteSpace(middlewareCustomer.CustomerVATId))
        {
            return false;
        }

        if(middlewareCustomer.CustomerCountry == null || middlewareCustomer.CustomerCountry.ToUpper() != "ES")
        {
            // Not a Spanish customer, skip validation
            return true;
        }

        var regex = new Regex("(([a-z|A-Z]{1}\\d{7}[a-z|A-Z]{1})|(\\d{8}[a-z|A-Z]{1})|([a-z|A-Z]{1}\\d{8}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (!regex.IsMatch(middlewareCustomer.CustomerVATId))
        {
            return false;
        }
        return true;
    }
}
