using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Test.Launcher.Extensions;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiskaltrust.Middleware.Test.Launcher.Helpers.IT;

class CashBoxBuilderIT : ICashBoxBuilder
{
    public string Market { get => "IT"; }
    public PackageConfiguration? _queueConfiguration;
    private IITSSCD? _itsscd;

    public void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId)
    {
        scuConfiguration.Url = ["http://localhost"];
        queueConfiguration.Configuration.Add(
                $"init_ftSignaturCreationUnitIT",
                new List<ftSignaturCreationUnitIT> {
                    new ftSignaturCreationUnitIT
                    {
                        ftSignaturCreationUnitITId = scuId,
                        Url = scuConfiguration.Url[0]
                    }
                }
        );

        var bootstrapper = scuConfiguration.Package switch
        {
            "fiskaltrust.Middleware.SCU.IT.CustomRTServer" => new SCU.IT.CustomRTServer.ScuBootstrapper(),
            _ => throw new NotImplementedException($"SCU package {scuConfiguration.Package} not implemented in test launcher")
        };
        bootstrapper.Id = scuId;
        scuConfiguration.Id = scuId;
        bootstrapper.Configuration = scuConfiguration.Configuration.NewtonsoftJsonWarp()!;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory.Create(builder => builder.AddConsole()));
        serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        bootstrapper.ConfigureServices(serviceCollection);
        _itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
    }
    public void AddMarketQueue(PackageConfiguration configuration, Guid queueId, Guid scuId)
    {
        configuration.Configuration.Add(
                $"init_ftQueueIT",
                new List<ftQueueIT> {
                    new ftQueueIT
                    {
                        ftQueueITId = queueId,
                        CashBoxIdentification = queueId.ToString(),
                        ftSignaturCreationUnitITId = scuId
                    }
                }
        );
        configuration.Id = queueId;
        _queueConfiguration = configuration;
    }

    public IPOS CreateIPOS()
    {
        if (_queueConfiguration is null)
        {
            throw new InvalidOperationException("Queue configuration is not set. Call AddMarketQueue first.");
        }
        if (_itsscd is null)
        {
            throw new InvalidOperationException("SCU is not set. Call AddSCU first.");
        }
        var bootstrapper = new Queue.InMemory.PosBootstrapper();
        bootstrapper.Id = _queueConfiguration.Id;
        bootstrapper.Configuration = _queueConfiguration.Configuration.NewtonsoftJsonWarp();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory.Create(builder => builder.AddConsole()));
        serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        serviceCollection.AddSingleton<IClientFactory<IITSSCD>>(new InMemoryClientFactory<IITSSCD>(_itsscd));
        bootstrapper.ConfigureServices(serviceCollection);

        return serviceCollection.BuildServiceProvider().GetRequiredService<IPOS>();
    }
}