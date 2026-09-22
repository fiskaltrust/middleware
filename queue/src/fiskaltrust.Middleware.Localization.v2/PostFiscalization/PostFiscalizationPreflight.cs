namespace fiskaltrust.Middleware.Localization.v2.PostFiscalization;

/// <summary>
/// Result of the preflight phase, carried from the preflight seam to the finalize seam of the
/// <see cref="SignProcessor"/>. When <see cref="Accepted"/> is false the receipt must not be fiscalized and
/// <see cref="Rejection"/> names the service and the reason.
/// </summary>
public sealed class PostFiscalizationPreflight
{
    /// <summary>Both sections are not configured: the mechanism is a no-op for this queue.</summary>
    public static PostFiscalizationPreflight Disabled { get; } = new()
    {
        EInvoicing = PostFiscalizationServiceOutcome.Disabled,
        EReporting = PostFiscalizationServiceOutcome.Disabled,
    };

    public required PostFiscalizationServiceOutcome EInvoicing { get; init; }

    public required PostFiscalizationServiceOutcome EReporting { get; init; }

    public PostFiscalizationRejection? Rejection { get; init; }

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
/// <param name="Service">The service that refused the receipt.</param>
/// <param name="Outcome">Rejected or Failed.</param>
/// <param name="Reason">Human-readable reason, used as the signature data: the service's errors, or the transport cause.</param>
/// <param name="Detail">Technical detail for the action journal.</param>
public sealed record PostFiscalizationRejection(PostFiscalizationService Service, PostFiscalizationServiceOutcome Outcome, string Reason, string? Detail)
{
    /// <summary>Stable, machine-matchable signature caption (<c>einvoicing-rejected</c> / <c>ereporting-rejected</c>).</summary>
    public string Caption => Service.RejectedCaption();
}
