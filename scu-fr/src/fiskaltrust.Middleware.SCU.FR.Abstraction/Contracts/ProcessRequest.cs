namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>The receipt pair handed to a French SCU for signing.</summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; set; }

    public required ReceiptResponse ReceiptResponse { get; set; }
}

/// <summary>The signed receipt returned by a French SCU.</summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; set; }
}
