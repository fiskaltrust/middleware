using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// Queue-level configuration of the optional eInvoicing and eReporting services (RFC 712). Both sections live in
/// the queue's <c>Configuration</c> dictionary next to keys like <c>scu-timeout-ms</c>:
/// <code>
/// { "einvoicing": { "service": "government-it", "timeout-ms": 15000, "max-retries": 1 },
///   "ereporting": { "service": "...", "timeout-ms": 15000, "max-retries": 1 } }
/// </code>
/// <c>service</c> names one of the <see cref="KnownPostFiscalizationServices"/>, whose endpoint and API version are
/// fixed in the middleware (sandbox queues use the sandbox endpoint). An explicit <c>endpoint</c> overrides the catalog
/// for local development and testing. Presence of a section enables the respective service; absence (the default)
/// disables it.
/// </summary>
public class PostFiscalizationConfiguration
{
    public const string EInvoicingKey = "einvoicing";
    public const string EReportingKey = "ereporting";

    [JsonProperty(EInvoicingKey)]
    public PostFiscalizationServiceConfiguration? EInvoicing { get; set; }

    [JsonProperty(EReportingKey)]
    public PostFiscalizationServiceConfiguration? EReporting { get; set; }

    [JsonIgnore]
    public bool IsEnabled => EInvoicing is not null || EReporting is not null;

    /// <summary>Parses the queue's configuration dictionary (the <c>Configuration</c> of its package configuration).</summary>
    public static PostFiscalizationConfiguration FromConfiguration(Dictionary<string, object>? configuration)
    {
        if (configuration is null)
        {
            return new PostFiscalizationConfiguration();
        }

        return new PostFiscalizationConfiguration
        {
            EInvoicing = ParseSection(configuration, EInvoicingKey),
            EReporting = ParseSection(configuration, EReportingKey),
        };
    }

    /// <summary>
    /// Fails loudly for a present but unusable section: a half-configured compliance feature must not silently
    /// no-op, and a misconfigured one must not leak the cashbox access token over plain http.
    /// </summary>
    /// <exception cref="PostFiscalizationConfigurationException">A configured section is invalid.</exception>
    public void Validate()
    {
        EInvoicing?.Validate(EInvoicingKey);
        EReporting?.Validate(EReportingKey);
    }

    private static PostFiscalizationServiceConfiguration? ParseSection(Dictionary<string, object> configuration, string key)
    {
        // The launcher hands the queue its configuration as Dictionary<string, object>; nested sections arrive as
        // Newtonsoft JObjects, System.Text.Json JsonElements or JSON strings depending on the host, so parse tolerantly.
        var entry = configuration.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
        if (entry.Key is null || entry.Value is null or JToken { Type: JTokenType.Null } or System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Null })
        {
            // Absent or explicitly null: the service is disabled.
            return null;
        }

        try
        {
            var section = entry.Value switch
            {
                string json => JsonConvert.DeserializeObject<PostFiscalizationServiceConfiguration>(json),
                JToken token => token.ToObject<PostFiscalizationServiceConfiguration>(),
                System.Text.Json.JsonElement element => JsonConvert.DeserializeObject<PostFiscalizationServiceConfiguration>(element.GetRawText()),
                _ => JsonConvert.DeserializeObject<PostFiscalizationServiceConfiguration>(JsonConvert.SerializeObject(entry.Value)),
            };
            return section ?? throw new PostFiscalizationConfigurationException($"The '{key}' configuration section is present but empty. Remove it to disable the service, or configure its 'service'.");
        }
        catch (JsonException ex)
        {
            throw new PostFiscalizationConfigurationException($"The '{key}' configuration section could not be parsed: {ex.Message}", ex);
        }
    }
}

/// <summary>Configuration of one eInvoicing or eReporting service.</summary>
public class PostFiscalizationServiceConfiguration
{
    public const long DefaultTimeoutMs = 15000;
    public const int DefaultMaxRetries = 1;

    /// <summary>Id of a known service (see <see cref="KnownPostFiscalizationServices"/>); endpoint and API version are fixed in the middleware.</summary>
    [JsonProperty("service")]
    public string? Service { get; set; }

    /// <summary>Optional endpoint override for local development and testing; takes precedence over <see cref="Service"/>.</summary>
    [JsonProperty("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>Timeout per attempt in milliseconds. Defaults to <see cref="DefaultTimeoutMs"/>.</summary>
    [JsonProperty("timeout-ms")]
    public long? TimeoutMs { get; set; }

    /// <summary>Number of additional attempts after the first. Defaults to <see cref="DefaultMaxRetries"/>.</summary>
    [JsonProperty("max-retries")]
    public int? MaxRetries { get; set; }

    [JsonIgnore]
    public TimeSpan Timeout => TimeSpan.FromMilliseconds(TimeoutMs ?? DefaultTimeoutMs);

    [JsonIgnore]
    public int EffectiveMaxRetries => MaxRetries ?? DefaultMaxRetries;

    /// <summary>Validates the section: a known service or a usable endpoint override, and sane timeout and retry values.</summary>
    /// <exception cref="PostFiscalizationConfigurationException">The section is invalid.</exception>
    public void Validate(string sectionName)
    {
        ResolveEndpoint(sectionName, isSandbox: true);

        if (TimeoutMs is <= 0)
        {
            throw new PostFiscalizationConfigurationException($"The '{sectionName}' 'timeout-ms' must be a positive number of milliseconds, but was {TimeoutMs}.");
        }

        if (MaxRetries is < 0)
        {
            throw new PostFiscalizationConfigurationException($"The '{sectionName}' 'max-retries' must not be negative, but was {MaxRetries}.");
        }
    }

    /// <summary>
    /// The endpoint the queue calls: the configured <c>endpoint</c> override if present, otherwise the known service's
    /// sandbox or production base with the section name appended as the concern (<c>.../v2/einvoicing</c>). An override
    /// must be https, because every call carries the cashbox access token; plain http is accepted for loopback addresses only.
    /// </summary>
    /// <exception cref="PostFiscalizationConfigurationException">Neither a known service nor a usable endpoint is configured.</exception>
    public Uri ResolveEndpoint(string sectionName, bool isSandbox)
    {
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            return ValidateEndpointOverride(sectionName, Endpoint!);
        }

        var known = KnownPostFiscalizationServices.Find(Service);
        if (known is not null)
        {
            return known.Endpoint(sectionName, isSandbox);
        }

        var knownIds = string.Join(", ", KnownPostFiscalizationServices.Ids.Select(id => $"'{id}'"));
        throw new PostFiscalizationConfigurationException(string.IsNullOrWhiteSpace(Service)
            ? $"The '{sectionName}' configuration section is present but names no 'service'. Configure one of {knownIds}, or an 'endpoint' override for local development, or remove the section to disable the service."
            : $"The '{sectionName}' service '{Service}' is unknown. Known services: {knownIds}.");
    }

    private static Uri ValidateEndpointOverride(string sectionName, string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new PostFiscalizationConfigurationException($"The '{sectionName}' endpoint '{endpoint}' is not a valid absolute URL.");
        }

        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            if (!uri.IsLoopback)
            {
                throw new PostFiscalizationConfigurationException($"The '{sectionName}' endpoint '{endpoint}' must use https. Every call carries the cashbox access token, so plain http is only accepted for loopback addresses (localhost, 127.0.0.0/8, ::1).");
            }
        }
        else if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new PostFiscalizationConfigurationException($"The '{sectionName}' endpoint '{endpoint}' uses the unsupported scheme '{uri.Scheme}'. Only https (and http for loopback addresses) is supported.");
        }

        return uri;
    }
}
