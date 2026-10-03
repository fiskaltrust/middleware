namespace fiskaltrust.Middleware.PostFiscalization.Contracts;

/// <summary>
/// An eReporting service validates a receipt before fiscalization and, afterwards, receives the fully fiscalized
/// receipt to transmit transactional data to an authority. See <see cref="ValidateRequest"/> for the provenance of
/// this type.
/// </summary>
public interface IEReportingService
{
    /// <summary>Preflight. Must not have side effects: no finalize call is guaranteed to follow.</summary>
    Task<ValidateResponse> ValidateReceiptAsync(ValidateRequest request);

    /// <summary>Finalize. Must be idempotent per <c>ftQueueItemID</c>, because the same request can arrive more than once.</summary>
    Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);
}
