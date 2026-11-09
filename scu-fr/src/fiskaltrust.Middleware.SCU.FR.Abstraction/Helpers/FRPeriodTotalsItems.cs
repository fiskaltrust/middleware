using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The readable totals a closing prints, as v1.2 did (workerFR "Day/Month/Year Totals" and the
/// perpetual "Totals" item): one item for the totals of the closed period and one for the perpetual
/// grand total, so the POS prints them without decoding the signed content. Every FR SCU emits
/// them the same way. A shift closing prints the perpetual total only: low word 0x0002 (v1.2 shift
/// totals) stays reserved.
/// </summary>
public static class FRPeriodTotalsItems
{
    public static void AddPeriodTotalsItems(this ReceiptResponse response, FRPeriodTotals? periodTotals)
    {
        if (periodTotals is null)
        {
            return;
        }

        var (type, caption) = periodTotals.Period switch
        {
            FRTotalsPeriod.Day => (SignatureTypeFR.DayTotals, "Day Totals"),
            FRTotalsPeriod.Month => (SignatureTypeFR.MonthTotals, "Month Totals"),
            FRTotalsPeriod.Year => (SignatureTypeFR.YearTotals, "Year Totals"),
            FRTotalsPeriod.Archive => (SignatureTypeFR.ArchiveTotals, "Archive Totals"),
            _ => ((SignatureTypeFR?) null, ""),
        };

        if (type is { } periodType)
        {
            response.AddSignatureItem(periodType, caption, Readable(periodTotals.Current));
        }

        response.AddSignatureItem(SignatureTypeFR.PerpetualTotals, "Totals", Readable(periodTotals.Perpetual));
    }

    /// <summary>The grand total (tax included), then its split by VAT rate and by payment nature, one per line.</summary>
    public static string Readable(FRTotals totals) => string.Join("\n",
        $"Total TTC: {Amount(totals.Totalizer)}",
        $"TVA 20 %: {Amount(totals.CINormal)}",
        $"TVA 10 %: {Amount(totals.CIReduced2)}",
        $"TVA 5,5 %: {Amount(totals.CIReduced1)}",
        $"TVA 2,1 % et autres taux: {Amount(totals.CIReducedS)}",
        $"TVA 0 %: {Amount(totals.CIZero)}",
        $"TVA inconnue: {Amount(totals.CIUnknown)}",
        $"Espèces: {Amount(totals.PICash)}",
        $"Autres paiements: {Amount(totals.PINonCash)}",
        $"Paiements internes: {Amount(totals.PIInternal)}",
        $"Paiements inconnus: {Amount(totals.PIUnknown)}");

    private static string Amount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
