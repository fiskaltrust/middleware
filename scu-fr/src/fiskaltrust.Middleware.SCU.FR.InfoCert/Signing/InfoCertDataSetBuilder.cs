using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// The 25-field data set (archive-v2 §5) the Infocert SCU signs on chain E (zero, one, lifecycle, SCU-switch and
/// migration receipts), field for field as the LNE and self-certified SCUs build it, as the payload of its JWS.
/// R13/R12/R19 define no string for these entries (market-fr#221).
/// </summary>
/// <remarks>
/// A twin of <c>LneDataSetBuilder</c> and <c>SelfCertifiedDataSetBuilder</c>: the SCUs are kept independent of each other,
/// and the structured fields (20) come from the shared <see cref="FRSignedRecords"/>. A change to the 25 fields changes
/// all three builders.
/// </remarks>
internal static class InfoCertDataSetBuilder
{
    public const char FieldSeparator = '|';

    public static string Build(ProcessRequest request, string? previousSignature, InfoCertConfiguration configuration)
    {
        var receiptRequest = request.ReceiptRequest;
        var response = request.ReceiptResponse;

        // A separator inside a field would shift every later field of the signed data set.
        FRIssuerClaims.RejectCharacter("Infocert", nameof(response.ftCashBoxIdentification), response.ftCashBoxIdentification, FieldSeparator);
        FRIssuerClaims.RejectCharacter("Infocert", nameof(response.ftReceiptIdentification), response.ftReceiptIdentification, FieldSeparator);
        FRIssuerClaims.RejectCharacter("Infocert", nameof(configuration.Rcs), configuration.Rcs, FieldSeparator);
        FRIssuerClaims.RejectCharacter("Infocert", nameof(configuration.Naf), configuration.Naf, FieldSeparator);
        FRIssuerClaims.RejectCharacter("Infocert", nameof(configuration.LegalForm), configuration.LegalForm, FieldSeparator);

        var chargeItems = receiptRequest.cbChargeItems ?? new List<ChargeItem>();
        var payItems = receiptRequest.cbPayItems ?? new List<PayItem>();

        var fields = new List<string>
        {
            configuration.Siret,
            configuration.Rcs ?? "",
            configuration.Naf ?? "",
            configuration.LegalForm ?? "",
            configuration.VatOption ?? "",
            // Field 6: reserved, formerly the seller's electronic address (BT-34), which stays out of the signed content.
            "",
            response.ftQueueID.ToString(),
            response.ftQueueItemID.ToString(),
            // ADR 0013: the canonical request value, as the LNE SCU signs it.
            FRCanonicalRequest.SignedField(receiptRequest),
            response.ftCashBoxIdentification ?? "",
            response.ftReceiptIdentification ?? "",
            response.ftReceiptMoment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ((long) receiptRequest.ftReceiptCase).ToString("X16", CultureInfo.InvariantCulture),
            FRStateData.CopyNumber(response)?.ToString(CultureInfo.InvariantCulture) ?? "",
            FRSalesLines.Count(receiptRequest).ToString(CultureInfo.InvariantCulture),
            receiptRequest.Currency.ToString(),
            Amount(chargeItems.Sum(x => x.Amount)),
            VatBreakdown(chargeItems),
            PaymentBreakdown(payItems),
            // Chosen by the receipt case (field 13): the migration event on 0x4022 (Finish migration, ADR 0003), the closed
            // periods on 0x4002 (Queue-stop receipt); empty elsewhere.
            FRSignedRecords.Field20(receiptRequest, response),
            // Every period the receipt closes, shortest first, then the perpetual totals (the 0x4002 stop signs day, month, year).
            FRSignedRecords.Field21(request.PeriodTotals),
            // The certificate serial, as in every 25-field data set; the JWS header names it too (kid).
            configuration.CertificateSerialNumber,
            request.ChainCounter.ToString(CultureInfo.InvariantCulture),
            FRChaining.Flag(previousSignature).ToCode(),
            previousSignature ?? "",
        };

        return string.Join(FieldSeparator, fields);
    }

    private static string VatBreakdown(List<ChargeItem> chargeItems)
        => string.Join(';', chargeItems
            .GroupBy(x => x.ftChargeItemCase.Vat())
            .OrderBy(x => (long) x.Key)
            .Select(x => $"{(long) x.Key:X}:{Amount(x.Sum(item => item.Amount))}"));

    private static string PaymentBreakdown(List<PayItem> payItems)
        => string.Join(';', payItems
            .GroupBy(x => x.ftPayItemCase.Case())
            .OrderBy(x => (long) x.Key)
            .Select(x => $"{(long) x.Key:X}:{Amount(x.Sum(item => item.Amount))}"));

    private static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
