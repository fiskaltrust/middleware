using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// Builds the per-rate VAT summary of a receipt (Infocert R13 v4.2 <i>Récapitulatif</i>:
/// ENC-TIK-TOT-MHT-TVA-TXX, ENC-TIK-TVA-TXX, ENC-TIK-TVA-MTN), which the archive carries for every
/// document type, notes included (fiskaltrust/market-fr#147).
/// </summary>
/// <remarks>
/// Every charge item counts, discounts, refunds and down payments included: VAT is due on a down
/// payment, so it belongs to the summary of the document that collected it. The VAT of an item is its
/// <see cref="ChargeItem.VATAmount"/> or, when absent, the share of its gross amount implied by its rate
/// - the same convention as the shared <c>ChargeItemExtensions.GetVATAmount</c>, which this assembly
/// cannot reference. The per-rate VAT is rounded to the cent (away from zero); the specification does
/// not state a rounding rule.
/// </remarks>
internal static class InfoCertVatSummary
{
    public static Dictionary<string, InfoCertVatRate>? From(ReceiptRequest request)
    {
        var chargeItems = request.cbChargeItems;
        if (chargeItems is null || chargeItems.Count == 0)
        {
            return null;
        }

        return chargeItems
            .GroupBy(x => x.VATRate)
            .OrderByDescending(x => x.Key)
            .ToDictionary(
                x => RateKey(x.Key),
                x =>
                {
                    var ttc = x.Sum(item => item.Amount);
                    var vat = Math.Round(x.Sum(VatOf), 2, MidpointRounding.AwayFromZero);
                    return new InfoCertVatRate { AmountTTC = ttc, VATAmount = vat, AmountHT = ttc - vat };
                });
    }

    /// <summary>The printed data of a summary line: <c>HT 10.00 | TVA 2.00 | TTC 12.00</c>.</summary>
    public static string Format(InfoCertVatRate line)
        => string.Create(CultureInfo.InvariantCulture, $"HT {line.AmountHT:0.00} | TVA {line.VATAmount:0.00} | TTC {line.AmountTTC:0.00}");

    /// <summary>The rate as a culture-invariant number without trailing zeros: 20 → "20", 5.50 → "5.5", 0 → "0".</summary>
    public static string RateKey(decimal rate) => rate.ToString("0.##########", CultureInfo.InvariantCulture);

    private static decimal VatOf(ChargeItem item) => item.VATAmount ?? item.Amount / (100 + item.VATRate) * item.VATRate;
}

/// <summary>
/// One line of a receipt's VAT summary: the amounts of every charge item that carries the same VAT
/// rate. <see cref="AmountTTC"/> is the sum of the (gross) charge item amounts,
/// <see cref="VATAmount"/> the sum of their VAT and <see cref="AmountHT"/> the difference, so
/// HT + VAT = TTC holds exactly on every line.
/// </summary>
internal class InfoCertVatRate
{
    public decimal AmountHT { get; set; }

    public decimal VATAmount { get; set; }

    public decimal AmountTTC { get; set; }
}
