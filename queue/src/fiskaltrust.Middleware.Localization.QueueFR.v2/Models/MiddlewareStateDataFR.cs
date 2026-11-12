using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using SharedStateData = fiskaltrust.Middleware.Localization.v2.Models.MiddlewareStateData;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// The <c>ftStateData</c> of a French receipt response: the shared middleware part (previous receipt
/// references) plus an <c>FR</c> block for what only France reports. Mirrors the pattern of the ES queue.
/// </summary>
public class MiddlewareStateData : SharedStateData
{
    public MiddlewareStateData() { }

    private MiddlewareStateData(SharedStateData middlewareStateData) : base(middlewareStateData)
    {
        // The shared type knows nothing of the FR block, so a block that came in as JSON sits in its
        // extension data. Lift it into the typed property so that it is never written twice.
        if (ExtraData.Remove("FR", out var fr))
        {
            FR = fr.Deserialize<MiddlewareStateDataFR>();
        }
    }

    [JsonPropertyName("FR")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MiddlewareStateDataFR? FR { get; set; }

    /// <summary>
    /// Reads the state data already on the response - the shared <c>SignProcessor</c> may have put the
    /// previous receipt references there - so that adding the FR block never drops it.
    /// </summary>
    public new static MiddlewareStateData FromReceiptResponse(ReceiptResponse receiptResponse) => receiptResponse.ftStateData switch
    {
        MiddlewareStateData fr => fr,
        JsonElement json => JsonSerializer.Deserialize<MiddlewareStateData>(json.GetRawText()) ?? new MiddlewareStateData(),
        SharedStateData shared => new MiddlewareStateData(shared),
        _ => new MiddlewareStateData(),
    };
}

public class MiddlewareStateDataFR
{
    /// <summary>
    /// What the receipt sold: <c>B</c> goods (biens), <c>S</c> services, <c>M</c> both. Omitted when the
    /// receipt has neither, see <see cref="Logic.FRTypeOfServiceCalculator"/>.
    /// </summary>
    [JsonPropertyName("ftTypeOfService")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ftTypeOfService { get; set; }

    /// <summary>
    /// The reprint number of a copy (0x3010): 1 for the first fiscal copy of the copied receipt
    /// (R13 ENC-DUP-PRN-NUM, market-fr#235). Every FR SCU reads it from here and signs it.
    /// Omitted on every other receipt and on a training copy.
    /// </summary>
    [JsonPropertyName(fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers.FRStateData.CopyNumberProperty)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CopyNumber { get; set; }

    /// <summary>
    /// The boundaries of the archive an archive receipt (0x3011) closes, as v1.2 signed them
    /// (market-fr#221). Every FR SCU reads them from here and signs them. Omitted on every other
    /// receipt and on a training archive receipt.
    /// </summary>
    [JsonPropertyName(fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers.FRStateData.ArchiveProperty)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public fiskaltrust.ifPOS.v2.fr.FRArchiveBoundaries? Archive { get; set; }

    /// <summary>
    /// The migration event a finish-migration receipt (0x4022) signs: each v1 chain's last counter and
    /// hash (market-fr ADR 0003). Omitted on every other receipt.
    /// </summary>
    [JsonPropertyName(fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers.FRStateData.MigrationProperty)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public fiskaltrust.ifPOS.v2.fr.FRMigrationEvent? Migration { get; set; }

    /// <summary>
    /// The receipt's totals as the queue computed them when it signed: the receipt's own totals on the
    /// sales chains, the period and perpetual totals on a closing. The archive reads them from here, so
    /// they are stored data and never recomputed at export (archive-v2 §1.2, §1.3).
    /// </summary>
    [JsonPropertyName("Totals")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FRStateTotals? Totals { get; set; }

    /// <summary>The computed values of each charge item, in request order (archive-v2 §2, D7).</summary>
    [JsonPropertyName("ChargeItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FRStateChargeItem>? ChargeItems { get; set; }

    /// <summary>The receipt's totals per VAT rate, keyed by the invariant rate string (archive-v2 §4).</summary>
    [JsonPropertyName("VATSummary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, FRStateVatSummaryEntry>? VATSummary { get; set; }
}

/// <summary>Totals TTC by VAT case and by pay-item class, named as the archive columns.</summary>
public class FRStateTotalsValues
{
    public decimal Totalizer { get; set; }
    public decimal CINormal { get; set; }
    public decimal CIDiscounted1 { get; set; }
    public decimal CIDiscounted2 { get; set; }
    public decimal CISuperReduced1 { get; set; }
    public decimal CIZero { get; set; }
    public decimal CIUnknown { get; set; }
    public decimal PICash { get; set; }
    public decimal PINonCash { get; set; }
    public decimal PIInternal { get; set; }
    public decimal PIUnknown { get; set; }
}

public class FRStateTotals : FRStateTotalsValues
{
    /// <summary>On a closing: the perpetual totals since the start of the chain. Absent otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FRStateTotalsValues? Perpetual { get; set; }
}

public class FRStateChargeItem
{
    public decimal Position { get; set; }
    public decimal VATAmount { get; set; }
    public decimal AmountHT { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? UnitPriceHT { get; set; }

    /// <summary>The two-digit nature-of-VAT code of <c>ftChargeItemCase</c> (D10).</summary>
    public string VATNature { get; set; } = "00";

    /// <summary>On a discount or extra: the <c>Position</c> of the sales line it applies to (D9).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? AppliesTo { get; set; }

    /// <summary>On a sales line: the sum of the discounts and extras that apply to it (D9).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DiscountAmount { get; set; }
}

public class FRStateVatSummaryEntry
{
    public decimal AmountHT { get; set; }
    public decimal VATAmount { get; set; }
    public decimal AmountTTC { get; set; }

    /// <summary>The nature-of-VAT codes of the rate's lines, distinct, comma-separated.</summary>
    public string VATNature { get; set; } = "";
}
