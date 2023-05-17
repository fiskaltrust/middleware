using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;
using fiskaltrust.Middleware.SCU.IT.Test.Launcher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TestLauncher
{
    public class Program
    {
        private static readonly string cashBoxId = "";
        private static readonly string accessToken = "";

        public static async Task Main()
        {
            var cashBoxConfiguration = await HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).ConfigureAwait(false);
            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Package = "fiskaltrust.Middleware.SCU.IT.Epson";
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

            Console.WriteLine("Press key to end program");
            Console.ReadLine();

        }
    }
}
