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

    public static FRTotals GetTotals(this ftQueueFR queueFR, string prefix)
    {
        var totals = new FRTotals();
        foreach (var (_, set, suffix) in _fields)
        {
            set(totals, (decimal) Column(prefix, suffix).GetValue(queueFR)!);
        }
        return totals;
    }

    public static void SetTotals(this ftQueueFR queueFR, string prefix, FRTotals totals)
    {
        foreach (var (get, _, suffix) in _fields)
        {
            Column(prefix, suffix).SetValue(queueFR, get(totals));
        }
    }

    private static PropertyInfo Column(string prefix, string suffix)
        => typeof(ftQueueFR).GetProperty(prefix + suffix) ?? throw new InvalidOperationException($"ftQueueFR has no column {prefix}{suffix}.");
}
