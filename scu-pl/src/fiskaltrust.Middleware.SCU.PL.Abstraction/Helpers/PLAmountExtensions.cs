using System;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;

/// <summary>
/// Polish fiscal devices exchange monetary amounts as integer grosze (1 PLN = 100 gr).
/// </summary>
public static class PLAmountExtensions
{
    public static long ToGrosze(this decimal amountPln) => (long)Math.Round(amountPln * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal GroszeToPln(this long amountGrosze) => amountGrosze / 100m;
}
