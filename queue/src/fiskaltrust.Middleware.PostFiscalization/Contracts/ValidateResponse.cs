namespace fiskaltrust.Middleware.PostFiscalization.Contracts;

/// <summary>
/// Answer of an eInvoicing or eReporting service to the preflight call. See <see cref="ValidateRequest"/> for the
/// provenance of this type.
/// </summary>
public class ValidateResponse
{
    /// <summary>
    /// Whether this service acts on this receipt at all. If false, the finalize call is skipped. Required on the wire:
    /// a body without it is malformed.
    /// </summary>
    public bool? Applies { get; set; }

    /// <summary>Empty when the receipt is accepted. Any entry rejects the receipt before fiscalization.</summary>
    public List<string> Errors { get; set; } = [];
}
