using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Utilities
{
    public class EpsonFormatter
    {
        public static NumberFormatInfo GetCurrencyFormatter()
        {
            return new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                CurrencyDecimalDigits = 2
            };
        }

        public static NumberFormatInfo GetQuantityFormatter()
        {
            return new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                NumberDecimalDigits = 3
            };
        }
    }
}
