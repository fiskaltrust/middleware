namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// Result of the preflight phase, carried from the preflight seam to the finalize seam of the sign processor. When
/// <see cref="Accepted"/> is false the receipt must not be fiscalized and <see cref="Rejection"/> names the service
/// and the reason.
/// </summary>
public sealed class PostFiscalizationPreflight
{
    /// <summary>Both sections are not configured: the mechanism is a no-op for this queue.</summary>
    public static PostFiscalizationPreflight Disabled { get; } = new(PostFiscalizationServiceOutcome.Disabled, PostFiscalizationServiceOutcome.Disabled, null);

    public PostFiscalizationPreflight(PostFiscalizationServiceOutcome eInvoicing, PostFiscalizationServiceOutcome eReporting, PostFiscalizationRejection? rejection)
    {
        EInvoicing = eInvoicing;
        EReporting = eReporting;
        Rejection = rejection;
    }

    public PostFiscalizationServiceOutcome EInvoicing { get; }

    public PostFiscalizationServiceOutcome EReporting { get; }

    public PostFiscalizationRejection? Rejection { get; }

    public bool Accepted => Rejection is null;

    public PostFiscalizationServiceOutcome this[PostFiscalizationService service] => service switch
    {
        PostFiscalizationService.EInvoicing => EInvoicing,
        PostFiscalizationService.EReporting => EReporting,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
    };
}

/// <summary>
/// Why a receipt was refused before fiscalization. <see cref="Outcome"/> is <see cref="PostFiscalizationServiceOutcome.Rejected"/>
/// for a validation rejection (the service answered with errors) and <see cref="PostFiscalizationServiceOutcome.Failed"/>
/// for a transport or protocol failure, which is treated exactly like a rejection because refusing before
/// fiscalization is the safe direction.
/// </summary>
public sealed class PostFiscalizationRejection
{
    public PostFiscalizationRejection(PostFiscalizationService service, PostFiscalizationServiceOutcome outcome, string reason, string? detail)
    {
        Service = service;
        Outcome = outcome;
        Reason = reason;
        Detail = detail;
    }

    /// <summary>The service that refused the receipt.</summary>
    public PostFiscalizationService Service { get; }

    /// <summary>Rejected or Failed.</summary>
    public PostFiscalizationServiceOutcome Outcome { get; }

    /// <summary>Human-readable reason, used as the signature data: the service's errors, or the transport cause.</summary>
    public string Reason { get; }

    /// <summary>Technical detail for the action journal.</summary>
    public string? Detail { get; }

    /// <summary>Stable, machine-matchable signature caption (<c>einvoicing-rejected</c> / <c>ereporting-rejected</c>).</summary>
    public string Caption => Service.RejectedCaption();
}
