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
    /// On a copy: the <see cref="DocumentType"/> of the copied receipt (R13 ENC-DUP-TYP, archive-v2 §1.2 <c>CopyType</c>).
    /// Archived, not signed (signing it waits on the InfoCert signed format). Omitted when the copied receipt is not stored here.
    /// </summary>
    [JsonPropertyName("CopyType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CopyType { get; set; }

    /// <summary>
    /// On a copy: the fiscal number (<c>ftReceiptIdentification</c>) of the copied receipt (R13 ENC-DUP-ORI-NUM); the POS
    /// reference stays <c>cbPreviousReceiptReference</c>. Archived, not signed. Omitted when the copied receipt is not stored here.
    /// </summary>
    [JsonPropertyName("CopiedReceiptIdentification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CopiedReceiptIdentification { get; set; }

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

    /// <summary>
    /// On the closing chains S, Z, M, Y and the technical chain E: which period the receipt closes (archive-v2
    /// §1.3, <see cref="Logic.FRClosingPeriod"/>). Archived, not signed. Omitted on every other receipt.
    /// </summary>
    [JsonPropertyName("Closing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FRStateClosing? Closing { get; set; }

    /// <summary>
    /// On the sales chains T, P, I, B, X: the kind of document, derived from <c>ftReceiptCase</c> (archive-v2 §1.2):
    /// <c>ticket</c>, <c>payment-proof</c>, <c>invoice</c>, <c>receipt-without-obligation</c>, <c>delivery-note</c>,
    /// <c>note</c>, <c>pro-forma</c> or <c>order</c>. A training document has the type of the document it simulates.
    /// </summary>
    [JsonPropertyName("DocumentType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentType { get; set; }

    /// <summary>On the sales chains: <c>sale</c>, <c>refund</c> or <c>void</c>, from the <c>ftReceiptCase</c> flags (archive-v2 §1.2).</summary>
    [JsonPropertyName("OperationType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OperationType { get; set; }

    /// <summary>On a receipt with charge items: the sum of its discount and extra items, line and receipt level (archive-v2 §1.2, D9).</summary>
    [JsonPropertyName("DiscountTotal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DiscountTotal { get; set; }

    /// <summary>On a receipt with charge items: the number of sales lines, as the SCU signs it (R13 ENC-TIK-LIG-NBR, #148).</summary>
    [JsonPropertyName("LineCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LineCount { get; set; }

    /// <summary>
    /// On an invoice to a French business buyer that sent no <c>CustomerEndpointId</c>: <c>0225:&lt;SIREN&gt;</c>, the SIREN
    /// being the first nine digits of its <c>CustomerTaxId</c> (archive-v2 §1.2 <i>Customer</i>, market-fr#140, BR-FR-12).
    /// </summary>
    [JsonPropertyName("CustomerEndpointId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerEndpointId { get; set; }

    /// <summary>The computed values of each charge item, in request order (archive-v2 §2, D7).</summary>
    [JsonPropertyName("ChargeItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FRStateChargeItem>? ChargeItems { get; set; }

    /// <summary>The receipt's totals per VAT rate, keyed by the invariant rate string (archive-v2 §4).</summary>
    [JsonPropertyName("VATSummary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, FRStateVatSummaryEntry>? VATSummary { get; set; }
}

/// <summary>The period a closing or technical receipt closes (archive-v2 §1.3), named as the archive columns.</summary>
public class FRStateClosing
{
    /// <summary><c>zero</c>, <c>shift</c>, <c>daily</c>, <c>monthly</c>, <c>yearly</c> or <c>lifecycle</c> (ADR 0002).</summary>
    public string ClosingType { get; set; } = "";

    /// <summary>Where the period's totals started: the previous closing of that period, else the queue's start. Absent when the receipt closes no period.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? PeriodStart { get; set; }

    /// <summary>The closing receipt's own moment. Absent when the receipt closes no period.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? PeriodEnd { get; set; }
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

    /// <summary>On a sales line: the <c>FR.DiscountCode</c> of the discounts that apply to it, joined by commas (D9).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DiscountCode { get; set; }

    /// <summary>On a sales line: the <c>FR.DiscountRate</c> of the discounts that apply to it, joined by commas (D9).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DiscountRate { get; set; }

    /// <summary>Whether the line counts in <see cref="MiddlewareStateDataFR.LineCount"/>, as the SCU counts it (archive-v2 §2).</summary>
    public bool CountsAsLine { get; set; }
}

public class FRStateVatSummaryEntry
{
    public decimal AmountHT { get; set; }
    public decimal VATAmount { get; set; }
    public decimal AmountTTC { get; set; }

    /// <summary>The nature-of-VAT codes of the rate's lines, distinct, comma-separated.</summary>
    public string VATNature { get; set; } = "";
}
