using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Keeps the in-memory SCU (SCU.FR.InMemory: an ephemeral key, no certificate, no legal weight) to local and
/// development runs (market-fr#221). Sandbox and production queues sign with a real FR SCU.
/// </summary>
/// <remarks>
/// <para>
/// The SCU is recognised by the certification body it reports (<c>InMemory</c>), so the guard holds whether the SCU
/// runs in the queue's process or behind a wrapper. A queue marks a local or development run with the configuration
/// key <see cref="LocalDevelopmentKey"/> set to <c>true</c>: the portal never sends it, the test launcher and the test
/// helpers set it. A sandbox queue (<c>sandbox</c> = true) is never a local run, whatever the key says.
/// </para>
/// <para>
/// The queue builds before its SCU may be reachable, so the SCU is asked on the first call and refused from then on:
/// every receipt comes back unsigned (StateFR.SigningUnavailableError) and archive signing fails. Echo and the SCU
/// information pass through. An SCU that cannot be reached yet is asked again on the next call.
/// </para>
/// </remarks>
public sealed class FRInMemoryScuGuard : IFRSSCD
{
    /// <summary>The configuration key that marks a local or development queue.</summary>
    public const string LocalDevelopmentKey = "FRLocalDevelopment";

    /// <summary>The certification body the in-memory SCU reports.</summary>
    public const string InMemoryBody = "InMemory";

    private readonly IFRSSCD _inner;
    private readonly bool _localDevelopment;
    private readonly ILogger _logger;
    private volatile bool _checked;
    private string? _refusal;

    public FRInMemoryScuGuard(IFRSSCD inner, bool localDevelopment, ILogger logger)
    {
        _inner = inner;
        _localDevelopment = localDevelopment;
        _logger = logger;
    }

    /// <summary>
    /// Local development: <see cref="LocalDevelopmentKey"/> is <c>true</c> and the queue is not a sandbox queue.
    /// </summary>
    public static bool IsLocalDevelopment(Dictionary<string, object> configuration, bool isSandbox)
        => !isSandbox
           && configuration.TryGetValue(LocalDevelopmentKey, out var raw)
           && bool.TryParse(raw?.ToString(), out var local)
           && local;

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => _inner.EchoAsync(echoRequest);

    public Task<FRSSCDInfo> GetInfoAsync() => _inner.GetInfoAsync();

    public async Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
        => await RefusalAsync().ConfigureAwait(false) is { } refusal
            ? request.ReceiptResponse.ToSigningFailure(refusal)
            : await _inner.ProcessReceiptAsync(request, previousSignature).ConfigureAwait(false);

    public async Task<FRSigningCertificate> GetSigningCertificateAsync()
        => await RefusalAsync().ConfigureAwait(false) is { } refusal
            ? FRSigningCertificate.Failed(refusal)
            : await _inner.GetSigningCertificateAsync().ConfigureAwait(false);

    public async Task<FRHashSignature> SignHashAsync(byte[] sha256)
        => await RefusalAsync().ConfigureAwait(false) is { } refusal
            ? FRHashSignature.Failed(refusal)
            : await _inner.SignHashAsync(sha256).ConfigureAwait(false);

    private async Task<string?> RefusalAsync()
    {
        if (_checked)
        {
            return _refusal;
        }

        FRSSCDInfo? info;
        try
        {
            info = await _inner.GetInfoAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Not reachable yet: the call fails on its own, and the SCU is asked again next time.
            return null;
        }

        if (!_localDevelopment && FRSSCDInfoHelper.GetCertificationBody(info) == InMemoryBody)
        {
            _refusal = $"The in-memory SCU only runs locally or in development (configuration key {LocalDevelopmentKey}); a sandbox or production queue needs a real FR SCU.";
            _logger.LogError("{Refusal} Nothing is signed.", _refusal);
        }
        _checked = true;
        return _refusal;
    }
}
