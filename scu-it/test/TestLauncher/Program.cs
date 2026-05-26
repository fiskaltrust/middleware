using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Test.Launcher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;

namespace TestLauncher
{
    public class Program
    {
        private static readonly string cashBoxId    = "4a0d8f34-c06c-4467-91e5-f25257f8bb77";
        private static readonly string accessToken  = "BBwwLfCVEjL2bQcYFGRpWWXrfS9iwTRUjX+C5a55TKVeVDCxHIXkTpcBKzQaBEC1UHe3ogdqRvvHP8LZFK1sazA=";

        public static async Task Main()
        {
            var cashBoxConfiguration = await HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).ConfigureAwait(false);
            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Package = "fiskaltrust.Middleware.SCU.IT.CustomRTPrinter";

            Console.WriteLine($"SCU Id       : {config.Id}");
            Console.WriteLine($"SCU Package  : {config.Package}");
            Console.WriteLine($"SCU Url      : {string.Join(", ", config.Url)}");
            Console.WriteLine($"Configuration: {Newtonsoft.Json.JsonConvert.SerializeObject(config.Configuration)}");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);
            var bootStrapper = new ScuBootstrapper
            {
                Id = config.Id,
                Configuration = config.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
            var provider = serviceCollection.BuildServiceProvider();
            var sscd = provider.GetRequiredService<IITSSCD>();

            HostingHelper.SetupServiceForObject(config, sscd, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("SCU in ascolto. Premi INVIO per terminare.");
            Console.ReadLine();
        }
    }
}
