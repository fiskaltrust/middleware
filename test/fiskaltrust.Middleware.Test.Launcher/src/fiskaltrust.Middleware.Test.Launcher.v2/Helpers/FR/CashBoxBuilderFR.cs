using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.FR;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

class CashBoxBuilderFR : ICashBoxBuilder
{
    /// <summary>A syntactically valid SIRET for local runs - the in-memory SCU never checks it.</summary>
    private const string Siret = "12345678901234";

    public string Market { get => "FR"; }

    public PackageConfiguration? _scuConfiguration { get; set; }

    public void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
            "init_ftSignaturCreationUnitFR",
            new List<ftSignaturCreationUnitFR> {
                new ftSignaturCreationUnitFR
                {
                    ftSignaturCreationUnitFRId = scuId,
                    Siret = Siret,
                }
            }
        );
        _scuConfiguration = scuConfiguration;
    }

    public void AddMarketQueue(ref PackageConfiguration queueConfiguration, Guid queueId, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
            "init_ftQueueFR",
            new List<ftQueueFR> {
                new ftQueueFR
                {
                    ftQueueFRId = queueId,
                    CashBoxIdentification = queueId.ToString().Substring(0, 18),
                    ftSignaturCreationUnitFRId = scuId,
                    Siret = Siret,
                }
            }
        );
    }

    public IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid queueId)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        IFRSSCD scu = _scuConfiguration!.Package switch
        {
            // The certified SCUs need real signature creation data, so the launcher only wires the
            // in-memory one; it produces a well-formed chain with an ephemeral key.
            "fiskaltrust.Middleware.SCU.FR.InMemory" => new InMemorySCU(Siret),
            _ => throw new NotImplementedException("SCU Type not implemented")
        };

        return new Localization.QueueFR.v2.QueueFRBootstrapper(
            queueId,
            loggerFactory,
            queueConfiguration.Configuration,
            new FRSSCDJsonWarper(scu),
            new InMemoryStorageProvider(loggerFactory, queueId, queueConfiguration.Configuration));
    }
}
