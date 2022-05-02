using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.Test.Launcher.Grpc;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace fiskaltrust.Middleware.Queue.Test.Launcher
{
    public static class Program
    {
        private static readonly string _cashBoxId = "74851323-a96e-48ab-ad20-29cb4d3def4d";
        private static readonly string _accessToken = "BBJe5Byqji+p1Q7tlNOfJuoMRkT09RRlb29FLej4Nmy9KAF5WveTYg+E+dZhIe1EYsglKA2jrTKRw6lY4d7EgEE=";
        private static readonly string _localization = "ME";

        public static void Main(string configurationFilePath = "", string serviceFolder = @"C:\ProgramData\fiskaltrust\service")
        {
            ftCashBoxConfiguration cashBoxConfiguration = null;
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


            if (_localization.Equals("ME"))
            {
                serviceCollection.AddScoped<IClientFactory<IMESSCD>, MESSCDClientFactory>();
                var key = "init_ftQueue";
                if (config.Configuration.ContainsKey(key))
                {
                    var queues = JsonConvert.DeserializeObject<List<ftQueue>>(config.Configuration[key].ToString());
                    queues.FirstOrDefault().CountryCode = "ME";
                    config.Configuration[key] = JsonConvert.SerializeObject(queues);
                }
                var temp = config.Configuration["init_ftQueueDE"];
                config.Configuration["init_ftQueueME"] = temp.ToString().Replace("DE", "ME");
                config.Configuration["init_ftSignaturCreationUnitME"] = config.Configuration["init_ftSignaturCreationUnitDE"];
            }
            else
            { 
                serviceCollection.AddScoped<IClientFactory<IDESSCD>, DESSCDClientFactory>();
            }

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
