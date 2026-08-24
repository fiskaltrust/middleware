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
}

/// <summary>The signed receipt returned by a French SCU.</summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class ProcessResponse
{
    public ReceiptResponse ReceiptResponse { get; set; } = null!;
}
