using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.PostFiscalization.Contracts;

/// <summary>
/// Body of the finalize call (<c>POST {endpoint}/process</c>) to an eInvoicing or eReporting service. The
/// <see cref="ReceiptResponse"/> is the fully fiscalized response including everything the queue and the SCU
/// produced. See <see cref="ValidateRequest"/> for the provenance of this type.
/// </summary>
public class ProcessRequest
{
    public ReceiptRequest ReceiptRequest { get; set; } = null!;
    public ReceiptResponse ReceiptResponse { get; set; } = null!;
}
