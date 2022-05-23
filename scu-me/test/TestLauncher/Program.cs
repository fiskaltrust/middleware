using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.FiscalizationService;
using fiskaltrust.Middleware.SCU.ME.Test.Launcher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestLauncher
{
    public class Program
    {
        private static readonly string cashBoxId = "";
        private static readonly string accessToken = "";
        private static readonly string certificatePath = "";
        private static readonly string certificatePassword = "";
        public static async Task Main()
        {
            var cashBoxConfiguration = await HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).ConfigureAwait(false);
            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Package = "fiskaltrust.Middleware.SCU.ME.FiscalizationService";
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
            var messcd = provider.GetRequiredService<IMESSCD>();
            HostingHelper.SetupServiceForObject(config, messcd, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();

        }

        private static Dictionary<string, object> CreateScuConfig()
        {
            var certificate = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);
            return new Dictionary<string, object>
            {
                { "Certificate",  Convert.ToBase64String(certificate.Export(X509ContentType.Pfx)) },
                { "PosOperatorAddress", "Mustergasse 88" },
                { "PosOperatorCountry", "ME" },
                { "PosOperatorName", "Hotel007" },
                { "PosOperatorTown", "Beachtown" },
                { "TIN", "03102955" },
                { "VatNumber", "1234567890" },
                { "Sandbox", "true" }
            };
        }
    }
}
