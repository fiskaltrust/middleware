namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// One line of a receipt's VAT summary (Infocert R13 v4.2 <i>Récapitulatif</i>): the amounts of
/// every charge item that carries the same VAT rate.
/// </summary>
/// <remarks>
/// Keyed by the rate in <see cref="ProcessResponse.VatSummary"/>. <see cref="AmountTTC"/> is the sum
/// of the charge item amounts (which are gross), <see cref="VATAmount"/> the sum of their VAT and
/// <see cref="AmountHT"/> the difference, so HT + VAT = TTC holds exactly on every line.
/// Temporary local declaration, see <see cref="IFRSSCD"/>.
/// </remarks>
public class FRVatRateSummary
{
    public decimal AmountHT { get; set; }

    public decimal VATAmount { get; set; }

    public decimal AmountTTC { get; set; }
}
