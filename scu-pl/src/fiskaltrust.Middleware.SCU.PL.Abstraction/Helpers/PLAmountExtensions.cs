using System;
using System.Globalization;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;

/// <summary>
/// Polish fiscal devices exchange monetary amounts as integer grosze (1 PLN = 100 gr).
/// </summary>
public static class PLAmountExtensions
{
    public static long ToGrosze(this decimal amountPln) => (long)Math.Round(amountPln * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal GroszeToPln(this long amountGrosze) => amountGrosze / 100m;

    /// <summary>
    /// The amount the way it reads on a receipt — two decimals, culture-independent — for messages
    /// that name a value the operator will compare against paper. The decimal on its own drops the
    /// grosze of a round amount ("10" for 10.00), which is exactly where a reader hesitates.
    /// </summary>
    public static string GroszeToPlnText(this long amountGrosze)
        => amountGrosze.GroszeToPln().ToString("0.00", CultureInfo.InvariantCulture);
}
