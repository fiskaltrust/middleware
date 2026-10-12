namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>The receipt pair handed to a French SCU for signing.</summary>
/// <remarks>
/// Temporary local declaration, see <see cref="IFRSSCD"/>. The properties are plain settable ones
/// rather than <c>required</c>, so the type stays constructible through <c>new()</c> - the json
/// round-tripping the SCU hosting does relies on that.
/// </remarks>
public class ProcessRequest
{
    public ReceiptRequest ReceiptRequest { get; set; } = null!;

    public ReceiptResponse ReceiptResponse { get; set; } = null!;

    /// <summary>
    /// Set for grand-total (closing) receipts only. A closing request carries no charge or pay
    /// items of its own - what it attests is the accumulated turnover of the period, which the
    /// queue owns and the SCU signs.
    /// </summary>
    public FRPeriodTotals? PeriodTotals { get; set; }

    /// <summary>
    /// The position of this entry in its receipt chain, starting at 1 — the numerator the queue
    /// also writes into <c>ftReceiptIdentification</c>. Every SCU signs it explicitly, so the
    /// signature authenticates the entry's place in the chain and not only its predecessor.
    /// </summary>
    public long ChainCounter { get; set; }

    /// <summary>
    /// The <c>requestHash</c> of the stored queue item: base64 SHA-256 over the exact request string
    /// the queue persisted (market-fr#225). It binds the signature to the request as received, so
    /// an SCU signs it as given and never re-serializes the request to recompute it. Null when the
    /// caller has no stored queue item.
    /// </summary>
    public string? RequestHash { get; set; }
}

/// <summary>The signed receipt returned by a French SCU.</summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class ProcessResponse
{
    public ReceiptResponse ReceiptResponse { get; set; } = null!;
}
