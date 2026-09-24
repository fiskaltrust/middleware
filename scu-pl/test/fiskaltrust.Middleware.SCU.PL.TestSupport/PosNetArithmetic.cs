namespace fiskaltrust.Middleware.SCU.PL.TestSupport;

/// <summary>
/// The arithmetic a register performs that a receipt does not spell out — shared by the device
/// model, which has to do what the printer does, and by the expected footprint, which has to
/// predict it.
/// </summary>
public static class PosNetArithmetic
{
    /// <summary>
    /// Splits a rabat/narzut od podsumy over the PTU rates in proportion to what was sold in each
    /// (POT-I-DEV-05 p.226: the register distributes it itself), in whole grosze. The shares are
    /// rounded by the largest-remainder method so that they add up to the amount exactly.
    /// </summary>
    /// <remarks>
    /// The proportional split is what the specification describes; how the register rounds the
    /// split is not documented and has not been measured yet — only single-rate receipts have been
    /// recorded so far. Until a multi-rate recording settles it, comparisons across rates allow one
    /// grosz of slack per slot (see <see cref="Verification.FootprintComparer"/>).
    /// </remarks>
    /// <param name="perRateGrosze">The value sold in each PTU slot before the modifier.</param>
    /// <param name="amountGrosze">The positive amount to distribute.</param>
    /// <returns>The share of each slot, summing to <paramref name="amountGrosze"/>.</returns>
    public static long[] Distribute(IReadOnlyList<long> perRateGrosze, long amountGrosze)
    {
        ArgumentNullException.ThrowIfNull(perRateGrosze);
        ArgumentOutOfRangeException.ThrowIfNegative(amountGrosze);

        var shares = new long[perRateGrosze.Count];
        var basis = perRateGrosze.Where(v => v > 0).Sum();
        if (basis == 0 || amountGrosze == 0)
        {
            return shares;
        }

        var remainders = new decimal[perRateGrosze.Count];
        long assigned = 0;
        for (var i = 0; i < perRateGrosze.Count; i++)
        {
            if (perRateGrosze[i] <= 0)
            {
                continue;
            }
            var exact = (decimal)amountGrosze * perRateGrosze[i] / basis;
            shares[i] = (long)decimal.Truncate(exact);
            remainders[i] = exact - shares[i];
            assigned += shares[i];
        }

        // Hand the grosze the truncation dropped to the slots that lost the most of them.
        foreach (var i in Enumerable.Range(0, perRateGrosze.Count)
                     .Where(i => perRateGrosze[i] > 0)
                     .OrderByDescending(i => remainders[i])
                     .ThenBy(i => i)
                     .Take((int)(amountGrosze - assigned)))
        {
            shares[i]++;
        }
        return shares;
    }
}
