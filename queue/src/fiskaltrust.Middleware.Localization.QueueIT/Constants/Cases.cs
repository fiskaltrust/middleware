using System.Globalization;

namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public class Cases
    {
        public const long BASE_STATE = 0x4954000000000000;

        public static NumberFormatInfo CurrencyFormatter = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };
    }
}