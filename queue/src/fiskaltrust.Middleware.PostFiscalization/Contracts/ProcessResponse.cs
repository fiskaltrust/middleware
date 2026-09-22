using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.PostFiscalization.Contracts;

/// <summary>
/// Answer of an eInvoicing or eReporting service to the finalize call. The returned <see cref="ReceiptResponse"/>
/// is used from then on. See <see cref="ValidateRequest"/> for the provenance of this type.
/// </summary>
public class ProcessResponse
{
    public ReceiptResponse ReceiptResponse { get; set; } = null!;
}
