namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>The two post-fiscalization concerns a queue can be configured with, in processing order.</summary>
public enum PostFiscalizationService
{
    EInvoicing,
    EReporting,
}

/// <summary>Outcome of one service for one receipt.</summary>
public enum PostFiscalizationServiceOutcome
{
    /// <summary>The section is not configured.</summary>
    Disabled,
    /// <summary>The preflight answered <c>Applies = false</c>; the finalize call is skipped.</summary>
    NotApplicable,
    /// <summary>The preflight accepted the receipt and the service acts on it; the finalize call is pending.</summary>
    Applies,
    /// <summary>The finalize call succeeded.</summary>
    Ok,
    /// <summary>The preflight rejected the receipt with validation errors. Nothing was fiscalized.</summary>
    Rejected,
    /// <summary>A transport or protocol failure in either phase, or a finalize call that reported an error.</summary>
    Failed,
    /// <summary>Not evaluated, because an earlier service already rejected the receipt.</summary>
    Skipped,
}

public static class PostFiscalizationServiceExtensions
{
    /// <summary>Configuration key and signature caption prefix (<c>einvoicing</c>, <c>ereporting</c>).</summary>
    public static string Key(this PostFiscalizationService service) => service switch
    {
        PostFiscalizationService.EInvoicing => PostFiscalizationConfiguration.EInvoicingKey,
        PostFiscalizationService.EReporting => PostFiscalizationConfiguration.EReportingKey,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
    };

    public static string DisplayName(this PostFiscalizationService service) => service switch
    {
        PostFiscalizationService.EInvoicing => "eInvoicing",
        PostFiscalizationService.EReporting => "eReporting",
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
    };

    /// <summary>Caption of the signature appended when the preflight rejects or fails: nothing was fiscalized.</summary>
    public static string RejectedCaption(this PostFiscalizationService service) => $"{service.Key()}-rejected";

    /// <summary>Caption of the signature appended when the finalize call fails: the receipt is fiscalized.</summary>
    public static string FailedCaption(this PostFiscalizationService service) => $"{service.Key()}-failed";

    /// <summary>Value of the <c>queue.PostFiscalization.*</c> activity tags.</summary>
    public static string ToTagValue(this PostFiscalizationServiceOutcome outcome) => outcome switch
    {
        PostFiscalizationServiceOutcome.Disabled => "disabled",
        PostFiscalizationServiceOutcome.NotApplicable => "not-applicable",
        PostFiscalizationServiceOutcome.Applies => "applies",
        PostFiscalizationServiceOutcome.Ok => "ok",
        PostFiscalizationServiceOutcome.Rejected => "rejected",
        PostFiscalizationServiceOutcome.Failed => "failed",
        PostFiscalizationServiceOutcome.Skipped => "skipped",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };
}
