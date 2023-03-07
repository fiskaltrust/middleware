using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson;
using fiskaltrust.Middleware.SCU.IT.Test.Launcher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestLauncher
{
    public class Program
    {
        private static readonly string cashBoxId = "e9df1360-fdbc-45b7-aebf-36ee72bb35a8";
        private static readonly string accessToken = "BPptoxY4DL714FFnYQakyxoEV2se0sTQ/zeIot9kHiLpbcVDIEc0i95zbsLEEEP53mcozErdRJVdwSQMLKIHHAs=";

        public static async Task Main()
        {
            var cashBoxConfiguration = await HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).ConfigureAwait(false);
            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Package = "fiskaltrust.Middleware.SCU.IT.Epson";
            config.Configuration = CreateScuConfig();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);
            var bootStrapper = new ScuBootstrapper
            {
                Id = config.Id,
                Configuration = config.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
            var provider = serviceCollection.BuildServiceProvider();
            var messcd = provider.GetRequiredService<IITSSCD>();
            HostingHelper.SetupServiceForObject(config, messcd, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();

        }

        private static Dictionary<string, object> CreateScuConfig()
        {
            return new Dictionary<string, object>
            {
                { "PrinterUrl", "127.0.0.1:4321" }
            };
        }
    }
}
