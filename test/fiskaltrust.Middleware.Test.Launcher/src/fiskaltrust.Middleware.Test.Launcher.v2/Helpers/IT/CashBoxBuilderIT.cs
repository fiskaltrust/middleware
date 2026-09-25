using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.IT;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

class CashBoxBuilderIT : ICashBoxBuilder
{
    public string Market { get => "IT"; }

    public PackageConfiguration? _scuConfiguration { get; set; }

    public void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId)
    {
        // The queue reads the SCU url from this table to create its client; the in-memory client factory below
        // ignores it, but an entry without a url would not even get that far.
        queueConfiguration.Configuration.AddUnlessConfigured(
                "init_ftSignaturCreationUnitIT",
                () => new List<ftSignaturCreationUnitIT> {
                    new ftSignaturCreationUnitIT
                    {
                        ftSignaturCreationUnitITId = scuId,
                        Url = "grpc://localhost:1400"
                    }
                }
        );
        _scuConfiguration = scuConfiguration;
    }

    public void AddMarketQueue(ref PackageConfiguration queueConfiguration, Guid queueId, Guid scuId)
    {
        queueConfiguration.Configuration.AddUnlessConfigured(
                "init_ftQueueIT",
                () => new List<ftQueueIT> {
                    new ftQueueIT
                    {
                        ftQueueITId = queueId,
                        CashBoxIdentification = queueId.ToString().Substring(0, 8),
                        ftSignaturCreationUnitITId = scuId
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

        IITSSCD scu = _scuConfiguration!.Package switch
        {
            "fiskaltrust.Middleware.SCU.IT.CustomRTServer" => CreateCustomRTServerSCU(_scuConfiguration, loggerFactory),
            _ => throw new NotImplementedException("SCU Type not implemented")
        };

        var clientFactory = new InMemoryClientFactory<IITSSCD>(new ITSSCDJsonWarper(scu));

        return new Localization.QueueIT.QueueITBootstrapper(
            queueId,
            loggerFactory,
            clientFactory,
            queueConfiguration.Configuration!,
            new InMemoryStorageProvider(loggerFactory, queueId, queueConfiguration.Configuration!));
    }

    /// <summary>
    /// Builds the Custom RT Server SCU the way the launcher does — through its bootstrapper, so the DI wiring
    /// under test is the wiring that runs here too.
    /// </summary>
    private static IITSSCD CreateCustomRTServerSCU(PackageConfiguration scuConfiguration, ILoggerFactory loggerFactory)
    {
        var bootstrapper = new SCU.IT.CustomRTServer.ScuBootstrapper
        {
            Id = scuConfiguration.Id,
            Configuration = scuConfiguration.Configuration.NewtonsoftJsonWarp()!,
        };
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        bootstrapper.ConfigureServices(services);
        return services.BuildServiceProvider().GetRequiredService<IITSSCD>();
    }
}
