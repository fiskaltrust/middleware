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
        private static readonly string cashBoxId = "74851323-a96e-48ab-ad20-29cb4d3def4d";
        private static readonly string accessToken = "BBJe5Byqji+p1Q7tlNOfJuoMRkT09RRlb29FLej4Nmy9KAF5WveTYg+E+dZhIe1EYsglKA2jrTKRw6lY4d7EgEE=";
        private static readonly string certificatePath = "C:\\Temp\\mw-me\\certificate.pfx";
        private static readonly string certificatePassword = "13816009";
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
                { "Certificate", Convert.ToBase64String(certificate.Export(X509ContentType.Pfx)) },
                { "PosOperatorAddress", "Mustergasse 88" },
                { "PosOperatorCountry", "ME" },
                { "PosOperatorName", "Hotel007" },
                { "PosOperatorTown", "Beachtown" },
                { "TIN", "03102955" },
                { "VatNumber", "1234567890" },
                { "Sandbox", "true" },
                { "DatetimeFormat", "UTC"}
            };
        }
    }
}
