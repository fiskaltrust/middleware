namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// The eInvoicing and eReporting services a queue can be configured with by name. Endpoint and API version are fixed
/// here, so a queue configuration says <em>which</em> service it uses, not where it lives; sandbox queues talk to the
/// sandbox endpoint. An explicit <c>endpoint</c> in the configuration overrides the catalog for local development.
/// </summary>
public static class KnownPostFiscalizationServices
{
    public sealed class KnownService
    {
        public KnownService(string id, string displayName, Uri sandboxEndpoint, Uri productionEndpoint)
        {
            Id = id;
            DisplayName = displayName;
            SandboxEndpoint = sandboxEndpoint;
            ProductionEndpoint = productionEndpoint;
        }

        /// <summary>The value of the <c>service</c> configuration key.</summary>
        public string Id { get; }

        public string DisplayName { get; }

        /// <summary>Endpoint incl. API version for sandbox queues.</summary>
        public Uri SandboxEndpoint { get; }

        /// <summary>Endpoint incl. API version for production queues.</summary>
        public Uri ProductionEndpoint { get; }

        public Uri Endpoint(bool isSandbox) => isSandbox ? SandboxEndpoint : ProductionEndpoint;
    }

    /// <summary>fiskaltrust's Italian eInvoicing service (FatturaPA via the SdI), API version v2 (the payload is the v2 contract).</summary>
    public const string GovernmentIt = "government-it";

    private static readonly Dictionary<string, KnownService> _services = new(StringComparer.OrdinalIgnoreCase)
    {
        // The production host follows the sandbox naming ("-sandbox" suffix dropped); confirm it before the first production rollout.
        [GovernmentIt] = new KnownService(GovernmentIt, "fiskaltrust eInvoicing Italy (FatturaPA)", new Uri("https://government-sandbox.fiskaltrust.it/v2"), new Uri("https://government.fiskaltrust.it/v2")),
    };

    /// <summary>The configurable service ids, for error messages.</summary>
    public static IReadOnlyCollection<string> Ids => _services.Keys.ToList();

    public static KnownService? Find(string? id) => id is not null && _services.TryGetValue(id.Trim(), out var service) ? service : null;
}
