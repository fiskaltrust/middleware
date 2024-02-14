using System;
using System.IO;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.Middleware.SCU.AT.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.AT.Test.Launcher
{
    public static class Program
    {
        private static readonly bool _useHelipad = false;
        private static readonly string _cashBoxId = "";
        private static readonly string _accessToken = "";

        private static readonly string _configurationFilePath = "";

        public static void Main(string serviceFolder = @"C:\ProgramData\fiskaltrust\service")
        {
            ftCashBoxConfiguration cashBoxConfiguration;
            if (_useHelipad)
            {
                cashBoxConfiguration = HelipadHelper.GetConfigurationAsync(_cashBoxId, _accessToken).Result;
            }
            else if (!string.IsNullOrEmpty(_configurationFilePath))
            {
                cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(File.ReadAllText(_configurationFilePath));
            }
            else
            {
                throw new Exception("No configuration file or helipad is set.");
            }

            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Configuration.Add("cashboxid", cashBoxConfiguration.ftCashBoxId);
            config.Configuration.Add("accesstoken", "");
            config.Configuration.Add("useoffline", true);
            config.Configuration.Add("sandbox", true);
            config.Configuration.Add("servicefolder", serviceFolder);
            config.Configuration.Add("configuration", JsonConvert.SerializeObject(cashBoxConfiguration));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);

            if (config.Package == "fiskaltrust.Middleware.SCU.AT.InMemory" || config.Package == "fiskaltrust.signing.pfx")
            {
                ConfigureIMemory(config, serviceCollection);
            }
            else
            {
                throw new NotSupportedException($"The given package {config.Package} is not supported.");
            }
            var provider = serviceCollection.BuildServiceProvider();

            var desscd = provider.GetRequiredService<IATSSCD>();
            HostingHelper.SetupServiceForObject(config, desscd, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();
        }

        private static void ConfigureIMemory(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new InMemory.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }
    }
}
