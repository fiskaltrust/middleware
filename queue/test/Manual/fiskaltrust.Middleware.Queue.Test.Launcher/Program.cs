using System;
using System.Collections.Generic;
using System.IO;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.Test.Launcher.Grpc;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace fiskaltrust.Middleware.Queue.Test.Launcher
{
    public static class Program
    {
        private static readonly string _cashBoxId = "779a8865-2985-45e1-b433-cd084797662e";
        private static readonly string _accessToken = "BK/yYA5X4X42qrCoiI6aGWKxAlThW46+c+AvUpttqQRr9llP9tpQcnuv94/Unf4gJdGG+GxC4ICwBxpZ+d6qTmk=";

        public static void Main(string configurationFilePath = "", string serviceFolder = @"C:\ProgramData\fiskaltrust\service")
        {
            ftCashBoxConfiguration cashBoxConfiguration;
            if (!string.IsNullOrEmpty(configurationFilePath))
            {
                cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(configurationFilePath);
            }
            else
            {
                cashBoxConfiguration = HelipadHelper.GetConfigurationAsync(_cashBoxId, _accessToken).Result;
            }
            if (string.IsNullOrEmpty(serviceFolder))
            {
                serviceFolder = Directory.GetCurrentDirectory();
            }
            var config = cashBoxConfiguration.ftQueues[0];
            config.Configuration.Add("cashboxid", cashBoxConfiguration.ftCashBoxId);
            config.Configuration.Add("accesstoken", "");
            config.Configuration.Add("useoffline", false);
            config.Configuration.Add("sandbox", true);
            config.Configuration.Add("servicefolder", serviceFolder);
            config.Configuration.Add("configuration", JsonConvert.SerializeObject(cashBoxConfiguration));
            config.Configuration.Add("ClosingTARExportTimeoutMin", 1);
            config.Configuration.Add("ClosingTARExportPollingDurationMs", 6000);
            //config.Configuration.Add("DisableClosingTARExport", true);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);
            serviceCollection.AddScoped<IClientFactory<IDESSCD>, DESSCDClientFactory>();

            if (config.Package == "fiskaltrust.Middleware.Queue.SQLite")
            {
                ConfigureSQLite(config, serviceCollection);
            }
            else
            {
                throw new NotSupportedException($"The given package {config.Package} is not supported.");
            }
            var provider = serviceCollection.BuildServiceProvider();
            var pos = provider.GetRequiredService<IPOS>();
            HostingHelper.SetupServiceForObject(config, pos, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();
        }
        private static void ConfigureSQLite(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new SQLite.PosBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }
    }
}
