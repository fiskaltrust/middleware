using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.LNE.Signing;

/// <summary>
/// Builds the "jeu de données à signer" — the ordered, delimited field sequence the LNE referential
/// signs. Order and formatting are part of the signature: a verifier rebuilds the exact same string
/// from the archived receipt, so nothing here may be reordered or reformatted once certified.
/// </summary>
/// <remarks>
/// The 25-field list is frozen since 2026-11-10; any change is a decision of its own. Amounts are
/// written with two decimals and an invariant decimal point, moments in round-trip UTC, so the
/// string is stable across cultures and machines.
/// </remarks>
internal static class LneDataSetBuilder
{
    /// <summary>Field separator. Never appears in the values: identifiers, numbers and moments, and issuer claims that <see cref="LneConfiguration"/> rejects when they contain it.</summary>
    public const char FieldSeparator = '|';

    public static string Build(ReceiptRequest request, ReceiptResponse response, LneConfiguration configuration, long chainCounter, string? previousSignature, FRPeriodTotals? periodTotals = null, int? copyNumber = null, FRArchiveBoundaries? archive = null)
    {
        // A separator inside a field would shift every later field of the signed data set.
        FRIssuerClaims.RejectCharacter("LNE", nameof(response.ftCashBoxIdentification), response.ftCashBoxIdentification, FieldSeparator);
        FRIssuerClaims.RejectCharacter("LNE", nameof(response.ftReceiptIdentification), response.ftReceiptIdentification, FieldSeparator);

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
            // market-fr#221: field 6 is reserved and always empty. It was the seller's electronic
            // address (BT-34), which is eInvoicing data and stays out of the signed content; it is kept so
            // nothing after it is renumbered.
            "",
            response.ftQueueID.ToString(),
            response.ftQueueItemID.ToString(),
            // market-fr#225, ADR 0013: the hash of the request in its canonical form, as version:base64.
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
            // TODO pending Louis (market-fr ADR 0003): where this data set carries the migration event of a
            // finish-migration receipt (0x4022), field 20 reused or a 26th field. Until then it is not signed here.
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
