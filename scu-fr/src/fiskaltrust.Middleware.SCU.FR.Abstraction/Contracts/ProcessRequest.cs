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
}

/// <summary>The signed receipt returned by a French SCU.</summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class ProcessResponse
{
    public ReceiptResponse ReceiptResponse { get; set; } = null!;
}
