using System.Reflection;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Where the fiscal state of a French queue lives in ftQueueFR (market-fr ADR 0012). A chain that
/// v1 already had keeps its v1 numerator, so a half-migrated queue continues counting without a
/// seed; the chains v1 does not have (ADR 0002) and the previous signature of every chain
/// (ADR 0001) have columns of their own.
/// </summary>
public static class FRQueueFRColumns
{
    public static long GetNumerator(this ftQueueFR queueFR, FRReceiptChain chain) => chain switch
    {
        FRReceiptChain.Ticket => queueFR.TNumerator,
        FRReceiptChain.PaymentProof => queueFR.PNumerator,
        FRReceiptChain.Invoice => queueFR.INumerator,
        FRReceiptChain.Technical => queueFR.ENumerator,
        FRReceiptChain.Bill => queueFR.BNumerator,
        FRReceiptChain.Log => queueFR.LNumerator,
        FRReceiptChain.Duplicate => queueFR.CNumerator,
        FRReceiptChain.Training => queueFR.XNumerator,
        FRReceiptChain.ShiftClosing => queueFR.SNumerator,
        FRReceiptChain.DailyClosing => queueFR.ZNumerator,
        FRReceiptChain.MonthlyClosing => queueFR.MNumerator,
        FRReceiptChain.YearlyClosing => queueFR.YNumerator,
        FRReceiptChain.Archive => queueFR.ANumerator,
        _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, null),
    };

    public static void SetNumerator(this ftQueueFR queueFR, FRReceiptChain chain, long numerator)
    {
        switch (chain)
        {
            case FRReceiptChain.Ticket: queueFR.TNumerator = numerator; break;
            case FRReceiptChain.PaymentProof: queueFR.PNumerator = numerator; break;
            case FRReceiptChain.Invoice: queueFR.INumerator = numerator; break;
            case FRReceiptChain.Technical: queueFR.ENumerator = numerator; break;
            case FRReceiptChain.Bill: queueFR.BNumerator = numerator; break;
            case FRReceiptChain.Log: queueFR.LNumerator = numerator; break;
            case FRReceiptChain.Duplicate: queueFR.CNumerator = numerator; break;
            case FRReceiptChain.Training: queueFR.XNumerator = numerator; break;
            case FRReceiptChain.ShiftClosing: queueFR.SNumerator = numerator; break;
            case FRReceiptChain.DailyClosing: queueFR.ZNumerator = numerator; break;
            case FRReceiptChain.MonthlyClosing: queueFR.MNumerator = numerator; break;
            case FRReceiptChain.YearlyClosing: queueFR.YNumerator = numerator; break;
            case FRReceiptChain.Archive: queueFR.ANumerator = numerator; break;
            default: throw new ArgumentOutOfRangeException(nameof(chain), chain, null);
        }
    }

    public static string? GetLastSignature(this ftQueueFR queueFR, FRReceiptChain chain)
    {
        var signature = chain switch
        {
            FRReceiptChain.Ticket => queueFR.TLastSignature,
            FRReceiptChain.PaymentProof => queueFR.PLastSignature,
            FRReceiptChain.Invoice => queueFR.ILastSignature,
            FRReceiptChain.Technical => queueFR.ELastSignature,
            FRReceiptChain.Bill => queueFR.BLastSignature,
            FRReceiptChain.Log => queueFR.LLastSignature,
            FRReceiptChain.Duplicate => queueFR.CLastSignature,
            FRReceiptChain.Training => queueFR.XLastSignature,
            FRReceiptChain.ShiftClosing => queueFR.SLastSignature,
            FRReceiptChain.DailyClosing => queueFR.ZLastSignature,
            FRReceiptChain.MonthlyClosing => queueFR.MLastSignature,
            FRReceiptChain.YearlyClosing => queueFR.YLastSignature,
            FRReceiptChain.Archive => queueFR.ALastSignature,
            _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, null),
        };
        return string.IsNullOrEmpty(signature) ? null : signature;
    }

    public static void SetLastSignature(this ftQueueFR queueFR, FRReceiptChain chain, string? signature)
    {
        switch (chain)
        {
            case FRReceiptChain.Ticket: queueFR.TLastSignature = signature; break;
            case FRReceiptChain.PaymentProof: queueFR.PLastSignature = signature; break;
            case FRReceiptChain.Invoice: queueFR.ILastSignature = signature; break;
            case FRReceiptChain.Technical: queueFR.ELastSignature = signature; break;
            case FRReceiptChain.Bill: queueFR.BLastSignature = signature; break;
            case FRReceiptChain.Log: queueFR.LLastSignature = signature; break;
            case FRReceiptChain.Duplicate: queueFR.CLastSignature = signature; break;
            case FRReceiptChain.Training: queueFR.XLastSignature = signature; break;
            case FRReceiptChain.ShiftClosing: queueFR.SLastSignature = signature; break;
            case FRReceiptChain.DailyClosing: queueFR.ZLastSignature = signature; break;
            case FRReceiptChain.MonthlyClosing: queueFR.MLastSignature = signature; break;
            case FRReceiptChain.YearlyClosing: queueFR.YLastSignature = signature; break;
            case FRReceiptChain.Archive: queueFR.ALastSignature = signature; break;
            default: throw new ArgumentOutOfRangeException(nameof(chain), chain, null);
        }
    }

    /// <summary>
    /// The v1 column prefix of a block of totals: the four periods are the v1 grand-total
    /// accumulators, and the ticket and invoice totals are never reset, so their sum is the
    /// perpetual total - exactly as v1 computes it.
    /// </summary>
    public static string TotalsPrefix(FRTotalsPeriod period) => period switch
    {
        FRTotalsPeriod.Shift => "GShift",
        FRTotalsPeriod.Day => "GDay",
        FRTotalsPeriod.Month => "GMonth",
        FRTotalsPeriod.Year => "GYear",
        FRTotalsPeriod.Archive => "A",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "The perpetual total is the sum of the ticket and invoice totals."),
    };

    public const string TicketTotalsPrefix = "T";

    public const string InvoiceTotalsPrefix = "I";

    // ftQueueFR names every block of totals the same way after its prefix, so the blocks are read
    // and written by name rather than through six copies of eleven assignments.
    private static readonly (Func<FRTotals, decimal> Get, Action<FRTotals, decimal> Set, string Suffix)[] _fields =
    [
        (x => x.Totalizer, (x, v) => x.Totalizer = v, "Totalizer"),
        (x => x.CINormal, (x, v) => x.CINormal = v, "CITotalNormal"),
        (x => x.CIReduced1, (x, v) => x.CIReduced1 = v, "CITotalReduced1"),
        (x => x.CIReduced2, (x, v) => x.CIReduced2 = v, "CITotalReduced2"),
        (x => x.CIReducedS, (x, v) => x.CIReducedS = v, "CITotalReducedS"),
        (x => x.CIZero, (x, v) => x.CIZero = v, "CITotalZero"),
        (x => x.CIUnknown, (x, v) => x.CIUnknown = v, "CITotalUnknown"),
        (x => x.PICash, (x, v) => x.PICash = v, "PITotalCash"),
        (x => x.PINonCash, (x, v) => x.PINonCash = v, "PITotalNonCash"),
        (x => x.PIInternal, (x, v) => x.PIInternal = v, "PITotalInternal"),
        (x => x.PIUnknown, (x, v) => x.PIUnknown = v, "PITotalUnknown"),
    ];

    /// <summary>
    /// A block of totals: the class columns, and the per-rate turnover kept in <see cref="FRStateV2.RateTotals"/>
    /// (market-fr#221, ADR 0021). A block v2 has never written (a queue migrated from v1.2) gets its per-rate turnover from its
    /// class columns, each class under its French rate (<see cref="RatesOfClasses"/>).
    /// </summary>
    public static FRTotals GetTotals(this ftQueueFR queueFR, string prefix)
    {
        var totals = new FRTotals();
        foreach (var (_, set, suffix) in _fields)
        {
            set(totals, (decimal) Column(prefix, suffix).GetValue(queueFR)!);
        }
        totals.ByRate = FRStateV2.Read(queueFR).RateTotals?.TryGetValue(prefix, out var byRate) == true
            ? new Dictionary<string, decimal>(byRate)
            : RatesOfClasses(totals);
        return totals;
    }

    public static void SetTotals(this ftQueueFR queueFR, string prefix, FRTotals totals)
    {
        foreach (var (get, _, suffix) in _fields)
        {
            Column(prefix, suffix).SetValue(queueFR, get(totals));
        }
        FRStateV2.Update(queueFR, state =>
        {
            state.RateTotals ??= new Dictionary<string, Dictionary<string, decimal>>();
            state.RateTotals[prefix] = totals.ByRate.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value);
        });
    }

    /// <summary>
    /// The class totals under their French rate: Normal 20, Reduced1 5.5, Reduced2 10, ReducedS 2.1, Zero 0, and the
    /// unknown class under <see cref="FRTotals.UnknownRateKey"/>. v1.2 kept only the classes, so a migrated block's
    /// ReducedS is taken as 2.1 % (the only super-reduced rate of metropolitan France).
    /// </summary>
    public static Dictionary<string, decimal> RatesOfClasses(FRTotals totals)
        => new (string Key, decimal Amount)[]
        {
            ("20", totals.CINormal), ("10", totals.CIReduced2), ("5.5", totals.CIReduced1), ("2.1", totals.CIReducedS),
            ("0", totals.CIZero), (FRTotals.UnknownRateKey, totals.CIUnknown),
        }.Where(x => x.Amount != 0).ToDictionary(x => x.Key, x => x.Amount);

    private static PropertyInfo Column(string prefix, string suffix)
        => typeof(ftQueueFR).GetProperty(prefix + suffix) ?? throw new InvalidOperationException($"ftQueueFR has no column {prefix}{suffix}.");
}
