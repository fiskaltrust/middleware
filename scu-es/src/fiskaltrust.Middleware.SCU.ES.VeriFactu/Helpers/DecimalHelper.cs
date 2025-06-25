using System.Globalization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;

public static class DecimalHelper
{
    public static string ToVeriFactuNumber(this Decimal from) => from.ToString("0.00", CultureInfo.InvariantCulture);
}