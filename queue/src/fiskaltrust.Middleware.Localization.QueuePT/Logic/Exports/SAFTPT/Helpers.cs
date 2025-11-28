using System.Globalization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT;

public static class Helpers
{
    public static decimal CreateTwoDigitMonetaryValue(decimal value) => decimal.Parse(value.ToString("F2", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    public static decimal CreateMonetaryValue(decimal value) => decimal.Parse(value.ToString("F6", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    public static decimal CreateMonetaryValue(decimal? value) => decimal.Parse(value.GetValueOrDefault().ToString("F6", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}