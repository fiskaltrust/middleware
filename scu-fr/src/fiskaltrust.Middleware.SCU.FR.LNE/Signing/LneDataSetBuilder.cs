using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.SCU.FR.LNE.Signing;

/// <summary>
/// Builds the "jeu de données à signer" — the ordered, delimited field sequence the LNE referential
/// signs. Order and formatting are part of the signature: a verifier rebuilds the exact same string
/// from the archived receipt, so nothing here may be reordered or reformatted once certified.
/// </summary>
/// <remarks>
/// The field list has to be confirmed against the LNE dossier before certification. Amounts are
/// written with two decimals and an invariant decimal point, moments in round-trip UTC, so the
/// string is stable across cultures and machines.
/// </remarks>
internal static class LneDataSetBuilder
{
    /// <summary>Field separator. Never appears in the values, which are all identifiers, numbers or moments.</summary>
    public const char FieldSeparator = '|';

    public static string Build(ReceiptRequest request, ReceiptResponse response, string siret, string certificateSerialNumber, string? lastHash, FRPeriodTotals? periodTotals = null)
    {
        var chargeItems = request.cbChargeItems ?? new List<ChargeItem>();
        var payItems = request.cbPayItems ?? new List<PayItem>();

        var fields = new List<string>
        {
            siret,
            response.ftQueueID.ToString(),
            response.ftQueueItemID.ToString(),
            response.ftCashBoxIdentification ?? "",
            response.ftReceiptIdentification ?? "",
            response.ftReceiptMoment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ((long) request.ftReceiptCase).ToString("X16", CultureInfo.InvariantCulture),
            request.Currency.ToString(),
            Amount(chargeItems.Sum(x => x.Amount)),
            VatBreakdown(chargeItems),
            PaymentBreakdown(payItems),
            PeriodTotals(periodTotals),
            certificateSerialNumber,
            lastHash ?? "",
        };

        return string.Join(FieldSeparator, fields);
    }

    /// <summary>
    /// The taxable bases grouped by VAT class, ascending by class, as <c>class:amount</c> pairs.
    /// Grouping keeps the data set the same length regardless of how many lines a receipt has.
    /// </summary>
    private static string VatBreakdown(List<ChargeItem> chargeItems)
        => string.Join(';', chargeItems
            .GroupBy(x => x.ftChargeItemCase.Vat())
            .OrderBy(x => (long) x.Key)
            .Select(x => $"{(long) x.Key:X}:{Amount(x.Sum(item => item.Amount))}"));

    /// <summary>The amounts grouped by payment nature, ascending by nature, as <c>nature:amount</c> pairs.</summary>
    private static string PaymentBreakdown(List<PayItem> payItems)
        => string.Join(';', payItems
            .GroupBy(x => x.ftPayItemCase.Case())
            .OrderBy(x => (long) x.Key)
            .Select(x => $"{(long) x.Key:X}:{Amount(x.Sum(item => item.Amount))}"));

    /// <summary>
    /// The accumulated totals of a grand-total (closing) receipt, as
    /// <c>period,current-fields,perpetual-fields</c>. The field is always present - empty on the
    /// receipts that are not closings - so the data set keeps a fixed shape.
    /// </summary>
    private static string PeriodTotals(FRPeriodTotals? periodTotals)
        => periodTotals is null ? "" : string.Join(',', new[] { periodTotals.Period.ToString() }.Concat(Fields(periodTotals.Current)).Concat(Fields(periodTotals.Perpetual)));

    private static IEnumerable<string> Fields(FRTotals totals) =>
    [
        Amount(totals.Totalizer),
        Amount(totals.CINormal),
        Amount(totals.CIReduced1),
        Amount(totals.CIReduced2),
        Amount(totals.CIReducedS),
        Amount(totals.CIZero),
        Amount(totals.CIUnknown),
        Amount(totals.PICash),
        Amount(totals.PINonCash),
        Amount(totals.PIInternal),
        Amount(totals.PIUnknown),
    ];

    private static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static byte[] ToBytes(string dataSet) => Encoding.UTF8.GetBytes(dataSet);
}
