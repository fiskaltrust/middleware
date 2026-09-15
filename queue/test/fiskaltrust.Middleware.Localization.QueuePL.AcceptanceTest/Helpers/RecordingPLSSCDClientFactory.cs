using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest.Helpers;

/// <summary>
/// Hands out a fixed SCU instance while recording the <see cref="ClientConfiguration"/> the
/// bootstrapper built — the tests assert that its Url is the one persisted in the configuration
/// repository, not something read from the init config.
/// </summary>
internal sealed class RecordingPLSSCDClientFactory(IPLSSCD sscd) : IClientFactory<IPLSSCD>
{
    private readonly IPLSSCD _sscd = sscd;

    public int CreateClientCalls { get; private set; }

    public ClientConfiguration? LastClientConfiguration { get; private set; }

    public IPLSSCD CreateClient(ClientConfiguration configuration)
    {
        CreateClientCalls++;
        LastClientConfiguration = configuration;
        return _sscd;
    }
}
