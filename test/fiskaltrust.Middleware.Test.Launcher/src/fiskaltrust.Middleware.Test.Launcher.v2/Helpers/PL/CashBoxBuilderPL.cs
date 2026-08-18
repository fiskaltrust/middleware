using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.PL.InMemory;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.PL;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

class CashBoxBuilderPL : ICashBoxBuilder
{
    public string Market { get => "PL"; }

    public PackageConfiguration? _scuConfiguration { get; set; }

    public void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
                $"init_ftSignaturCreationUnitPL",
                new List<ftSignaturCreationUnitPL> {
                    new ftSignaturCreationUnitPL
                    {
                        ftSignaturCreationUnitPLId = scuId,
                    }
                }
        );
        _scuConfiguration = scuConfiguration;
    }

    public void AddMarketQueue(ref PackageConfiguration queueConfiguration, Guid queueId, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
                $"init_ftQueuePL",
                new List<ftQueuePL> {
                    new ftQueuePL
                    {
                        ftQueuePLId = queueId,
                        CashBoxIdentification = queueId.ToString().Substring(0, 18),
                        ftSignaturCreationUnitPLId = scuId
                    }
                }
        );
    }

    public IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid queueId)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        IPLSSCD scu = _scuConfiguration!.Package switch
        {
            "fiskaltrust.Middleware.SCU.PL.InMemory" => new InMemorySCU(),
            "fiskaltrust.Middleware.SCU.PL.PosNet" => CreatePosNetSCU(_scuConfiguration),
            _ => throw new NotImplementedException("SCU Type not implemented")
        };

        return new Localization.QueuePL.QueuePLBootstrapper(
            queueId,
            loggerFactory,
            queueConfiguration.Configuration,
            new PLSSCDJsonWarper(scu),
            new InMemoryStorageProvider(loggerFactory, queueId, queueConfiguration.Configuration));
    }

    /// <summary>
    /// Builds the PosNet SCU the way the launcher does — through its bootstrapper, so the DI wiring
    /// under test is the wiring that runs here too.
    /// </summary>
    private static IPLSSCD CreatePosNetSCU(PackageConfiguration scuConfiguration)
    {
        var bootstrapper = new SCU.PL.PosNet.ScuBootstrapper
        {
            Id = scuConfiguration.Id,
            Configuration = scuConfiguration.Configuration.ToSystemTextJsonValues(),
        };
        var services = new ServiceCollection();
        bootstrapper.ConfigureServices(services);
        return services.BuildServiceProvider().GetRequiredService<IPLSSCD>();
    }
}
