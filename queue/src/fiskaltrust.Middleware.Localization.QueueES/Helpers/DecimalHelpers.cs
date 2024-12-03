using System.Globalization;

namespace fiskaltrust.Middleware.Localization.QueueES.Helpers;

public static class DecimalHelpers
{
    public static string ToVeriFactuNumber(this Decimal from) => from.ToString("0.00", CultureInfo.InvariantCulture);
}