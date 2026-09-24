namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// A call to an eInvoicing or eReporting service did not produce a usable answer: unreachable, timed out, non-2xx
/// after retries, malformed body, or a response that signals a failure. <see cref="Message"/> is the short,
/// human-readable cause that ends up in the failure signature; <see cref="Detail"/> carries the technical detail
/// for the action journal.
/// </summary>
public class PostFiscalizationServiceException : Exception
{
    public PostFiscalizationServiceException(string reason, string? detail = null, Exception? innerException = null) : base(reason, innerException)
    {
        Detail = detail;
    }

    public string? Detail { get; }
}
