using System.Globalization;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public static class ITConstants
{
    public const long BASE_STATE = 0x4954_2000_0000_0000;

    public static NumberFormatInfo CurrencyFormatter => new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "",
        CurrencyDecimalDigits = 2
    };
}
