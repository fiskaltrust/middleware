using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.Localization.v2.Models;

/// <summary>
/// Persisted outcome of the eInvoicing/eReporting phase (RFC 712), written by the middleware into
/// <c>ftStateData.PostFiscalization</c> after the last service returned so that the queue item records what was and
/// wasn't invoiced or reported, regardless of whether a service chose to add signatures.
/// <see cref="FiscalizationSucceeded"/> is what lets <c>ReceiptResponse.IsFiscalized()</c> recognize a receipt that
/// is fiscalized but carries an error state because a finalize call failed.
/// </summary>
public class PostFiscalizationStateData
{
    public const string Ok = "ok";
    public const string NotApplicable = "not-applicable";
    public const string Failed = "failed";
    public const string Disabled = "disabled";

    [JsonPropertyName("FiscalizationSucceeded")]
    public bool FiscalizationSucceeded { get; set; }

    /// <summary>One of <see cref="Ok"/>, <see cref="NotApplicable"/>, <see cref="Failed"/> or <see cref="Disabled"/>.</summary>
    [JsonPropertyName("EInvoicing")]
    public string EInvoicing { get; set; } = Disabled;

    /// <summary>One of <see cref="Ok"/>, <see cref="NotApplicable"/>, <see cref="Failed"/> or <see cref="Disabled"/>.</summary>
    [JsonPropertyName("EReporting")]
    public string EReporting { get; set; } = Disabled;
}
