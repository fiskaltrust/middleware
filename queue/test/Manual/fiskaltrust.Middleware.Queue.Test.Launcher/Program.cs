using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace fiskaltrust.Middleware.Queue.Test.Launcher
{
    public static class Program
    {
        private static readonly string _cashBoxId = "";
        private static readonly string _accessToken = "";
        private static readonly string _localization = "";

        public static void Main(string configurationFilePath = "C:\\Temp\\ATLauncher\\configuration.json", string serviceFolder = @"C:\ProgramData\fiskaltrust\service")
        {
            ftCashBoxConfiguration cashBoxConfiguration = null;
            if (!string.IsNullOrEmpty(configurationFilePath))
            {
                cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(File.ReadAllText(configurationFilePath));
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
            config.Configuration.Add("accesstoken", _accessToken);
            config.Configuration.Add("useoffline", true);
            config.Configuration.Add("sandbox", true);
            config.Configuration.Add("servicefolder", serviceFolder);
            config.Configuration.Add("configuration", JsonConvert.SerializeObject(cashBoxConfiguration));
            config.Configuration.Add("ClosingTARExportTimeoutMin", 1);
            config.Configuration.Add("ClosingTARExportPollingDurationMs", 6000);

            //config.Configuration.Add("DisableClosingTARExport", true);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);


            if (!string.IsNullOrEmpty(_localization))
            {
                if (_localization == "ME")
                {
                    serviceCollection.AddScoped<IClientFactory<IMESSCD>, MESSCDClientFactory>();
                    OverrideMasterdata(_localization, config);
                }
                else if (_localization == "IT")
                {
                    serviceCollection.AddScoped<IClientFactory<IITSSCD>, ITSSCDClientFactory>();
                }
                else if (_localization == "AT")
                {
                    serviceCollection.AddScoped<IClientFactory<IATSSCD>, ATSSCDClientFactory>();
                }
            }
            else
            {
                serviceCollection.AddScoped<IClientFactory<IDESSCD>, DESSCDClientFactory>();
            }

            if (config.Package == "fiskaltrust.Middleware.Queue.SQLite" || config.Package == "fiskaltrust.service.sqlite")
            {
                ConfigureSQLite(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.Queue.EF")
            {
                ConfigureEF(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.Queue.MySQL")
            {
                ConfigureMySQL(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.Queue.AzureTableStorage")
            {
                ConfigureAzureTableStorage(config, serviceCollection);
            }
            else
            {
                throw new NotSupportedException($"The given package {config.Package} is not supported.");
            }
            var provider = serviceCollection.BuildServiceProvider();

            var pos = provider.GetRequiredService<ifPOS.v1.IPOS>();
            HostingHelper.SetupServiceForObject(config, pos, provider.GetRequiredService<ILoggerFactory>());

            Console.WriteLine("Press key to end program");
            Console.ReadLine();
        }

        private static void OverrideMasterdata(string localization, PackageConfiguration config)
        {
            var key = "init_ftQueue";
            if (config.Configuration.ContainsKey(key))
            {
                var queues = JsonConvert.DeserializeObject<List<ftQueue>>(config.Configuration[key].ToString());
                queues.FirstOrDefault().CountryCode = localization;
                config.Configuration[key] = JsonConvert.SerializeObject(queues);
            }
            var temp = config.Configuration["init_ftQueueDE"];
            config.Configuration["init_ftQueue" + localization] = temp.ToString().Replace("DE", localization);
            temp = config.Configuration["init_ftSignaturCreationUnitDE"];
            config.Configuration["init_ftSignaturCreationUnit" + localization] = temp.ToString().Replace("DE", localization);

            var masterDataConfiguration = new MasterDataConfiguration
            {
                Account = new AccountMasterData { TaxId = "03102955" },
                Outlet = new OutletMasterData { LocationId = "pg000qi813" },
                PosSystems = new List<PosSystemMasterData>
                    {
                        new() { Brand = "xl522hw351", Model = "wv720nq953" }
                    }
            };
            config.Configuration["init_masterData"] = JsonConvert.SerializeObject(masterDataConfiguration);
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

        private static void ConfigureMySQL(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new MySQL.PosBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureAzureTableStorage(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new AzureTableStorage.PosBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureEF(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new EF.PosBootstrapper

            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }
    }
}
