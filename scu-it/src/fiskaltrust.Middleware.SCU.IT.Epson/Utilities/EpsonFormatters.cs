using System.Globalization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Utilities
{
    public class EpsonFormatters
    {
        public static NumberFormatInfo CurrencyFormatter =
            new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                CurrencyDecimalDigits = 2
            };

        public static NumberFormatInfo QuantityFormatter =
            new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                NumberDecimalDigits = 3
            };
    }
}
