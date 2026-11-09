using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.SelfCertified.Signing;

/// <summary>
/// Builds the "jeu de données à signer" of a self-certified installation — the same ordered,
/// delimited field sequence the LNE SCU signs. Order and formatting are part of the signature: a
/// verifier rebuilds the exact same string from the archived receipt, so nothing here may be
/// reordered or reformatted once in use.
/// </summary>
/// <remarks>
/// A copy of the LNE builder, not a shared one (market-fr#176): the SCUs are kept independent so a
/// change made for one certification body's audit cannot silently alter another's signed data.
/// Amounts are written with two decimals and an invariant decimal point, moments in round-trip UTC,
/// so the string is stable across cultures and machines.
/// </remarks>
internal static class SelfCertifiedDataSetBuilder
{
    /// <summary>Field separator. Never appears in the values: identifiers, numbers and moments, and issuer claims that <see cref="SelfCertifiedConfiguration"/> rejects when they contain it.</summary>
    public const char FieldSeparator = '|';

    public static string Build(ReceiptRequest request, ReceiptResponse response, SelfCertifiedConfiguration configuration, long chainCounter, string? previousSignature, FRPeriodTotals? periodTotals = null, int? copyNumber = null, FRArchiveBoundaries? archive = null)
    {
        var chargeItems = request.cbChargeItems ?? new List<ChargeItem>();
        var payItems = request.cbPayItems ?? new List<PayItem>();

        var fields = new List<string>
        {
            configuration.Siret,
            // market-fr#141: the issuer claims, at fixed positions after the SIRET and empty when
            // not configured, so the data set keeps one shape. The SIREN is the SIRET's first nine digits.
            configuration.Rcs ?? "",
            configuration.Naf ?? "",
            configuration.LegalForm ?? "",
            configuration.VatOption ?? "",
            configuration.ElectronicAddress ?? "",
            response.ftQueueID.ToString(),
            response.ftQueueItemID.ToString(),
            // market-fr#225: the stored request's hash, as the queue computed it; empty without one.
            FRCanonicalRequest.SignedField(request),
            response.ftCashBoxIdentification ?? "",
            response.ftReceiptIdentification ?? "",
            response.ftReceiptMoment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ((long) request.ftReceiptCase).ToString("X16", CultureInfo.InvariantCulture),
            // market-fr#235: the reprint number of a copy (R13 ENC-DUP-PRN-NUM); empty on every other receipt.
            copyNumber?.ToString(CultureInfo.InvariantCulture) ?? "",
            // market-fr#148, ADR 0011: the number of sales lines (R13 ENC-TIK-LIG-NBR), printed by the
            // queue as "Nombre de lignes" and signed here; 0 when the receipt has none.
            FRSalesLines.Count(request).ToString(CultureInfo.InvariantCulture),
            request.Currency.ToString(),
            Amount(chargeItems.Sum(x => x.Amount)),
            VatBreakdown(chargeItems),
            PaymentBreakdown(payItems),
            // market-fr#221: the boundaries an archive receipt signs, as v1.2; empty on every other
            // receipt. Placed before the period totals so the fields read from the end keep their place.
            FRArchiveBoundaries.SignedField(archive),
            PeriodTotals(periodTotals),
            configuration.CertificateSerialNumber,
            // ADR 0001: the chain counter, the start-of-chain flag and the previous entry's
            // signature, verbatim as its signature field stores it.
            chainCounter.ToString(CultureInfo.InvariantCulture),
            FRChaining.Flag(previousSignature).ToCode(),
            previousSignature ?? "",
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
