using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.service.launcher.Helpers.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.TestLauncher
{
    public static class Program
    {
        public static void Main()
        {
            var bootStrapper = new Swissbit.ScuBootstrapper
            {
                Id = Guid.Parse("fec8700a-2af8-4de9-acf0-fb73acc00b24"),

                Configuration = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "devicePath", "E:" }
                }
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);
            bootStrapper.ConfigureServices(serviceCollection);
            var provider = serviceCollection.BuildServiceProvider();
            var desscd = provider.GetRequiredService<IDESSCD>();

            var host = new GrpcHost(provider.GetRequiredService<ILogger<GrpcHost>>());
            host.StartService(new storage.serialization.V0.PackageConfiguration
            {
                Url = new string[] { "grpc://localhost:10301" },
                Id = Guid.Parse("fec8700a-2af8-4de9-acf0-fb73acc00b24"),
                Package = "fiskaltrust.Middleware.SCU.DE.Swissbit",
                Version = "1.3.1-rc1"
            }, "grpc://localhost:10301", desscd.GetType(), desscd, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();
        }
    }
}
