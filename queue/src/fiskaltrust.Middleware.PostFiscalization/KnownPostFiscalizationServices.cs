namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// The eInvoicing and eReporting services a queue can be configured with by name. Host and API version are fixed
/// here, so a queue configuration says <em>which</em> service it uses, not where it lives; sandbox queues talk to the
/// sandbox host. The concern a service is configured under (<c>einvoicing</c> / <c>ereporting</c>) is appended as a
/// path segment, and the client appends <c>/validate</c> and <c>/process</c>: <c>{base}/{concern}/validate</c>.
/// An explicit <c>endpoint</c> in the configuration overrides the catalog for local development.
/// </summary>
public static class KnownPostFiscalizationServices
{
    public sealed class KnownService
    {
        public KnownService(string id, string displayName, Uri sandboxBase, Uri productionBase)
        {
            Id = id;
            DisplayName = displayName;
            SandboxBase = sandboxBase;
            ProductionBase = productionBase;
        }

        /// <summary>The value of the <c>service</c> configuration key.</summary>
        public string Id { get; }

        public string DisplayName { get; }

        /// <summary>Host and API version for sandbox queues, e.g. <c>https://government-sandbox.fiskaltrust.it/v2</c>.</summary>
        public Uri SandboxBase { get; }

        /// <summary>Host and API version for production queues.</summary>
        public Uri ProductionBase { get; }

        /// <summary>The endpoint for one concern: <c>{base}/{concern}</c>, e.g. <c>https://government-sandbox.fiskaltrust.it/v2/einvoicing</c>.</summary>
        public Uri Endpoint(string concern, bool isSandbox)
        {
            var baseUri = isSandbox ? SandboxBase : ProductionBase;
            return new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}/{concern.Trim('/')}");
        }
    }

    /// <summary>fiskaltrust's Italian government services (FatturaPA via the SdI), API version v2 (the payload is the v2 contract).</summary>
    public const string GovernmentIt = "government-it";

    private static readonly Dictionary<string, KnownService> _services = new(StringComparer.OrdinalIgnoreCase)
    {
        // The production host follows the sandbox naming ("-sandbox" suffix dropped); confirm it before the first production rollout.
        [GovernmentIt] = new KnownService(GovernmentIt, "fiskaltrust government services Italy (FatturaPA)", new Uri("https://government-sandbox.fiskaltrust.it/v2"), new Uri("https://government.fiskaltrust.it/v2")),
    };

    /// <summary>The configurable service ids, for error messages.</summary>
    public static IReadOnlyCollection<string> Ids => _services.Keys.ToList();

    public static KnownService? Find(string? id) => id is not null && _services.TryGetValue(id.Trim(), out var service) ? service : null;
}
