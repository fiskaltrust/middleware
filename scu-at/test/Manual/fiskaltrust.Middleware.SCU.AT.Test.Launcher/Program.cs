using fiskaltrust.ifPOS.v2.at;
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
        private static readonly bool useHelipad = false;
        private static readonly string cashBoxId = "";
        private static readonly string accessToken = "";

        private static readonly string configurationFilePath = "";
        private static readonly string serviceFolder = Directory.GetCurrentDirectory();

        public static void Main()
        {
            ftCashBoxConfiguration cashBoxConfiguration;
            if (useHelipad)
            {
                cashBoxConfiguration = HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).Result;
            }
            else if (string.IsNullOrEmpty(configurationFilePath))
            {
                cashBoxConfiguration = GetDemoConfiguration();
            }
            else
            {
                cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(configurationFilePath);
            }

            var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
            config.Configuration.Add("cashboxid", cashBoxConfiguration.ftCashBoxId);
            config.Configuration.Add("accesstoken", "");
            config.Configuration.Add("useoffline", false);
            config.Configuration.Add("sandbox", true);
            config.Configuration.Add("servicefolder", serviceFolder);
            config.Configuration.Add("configuration", JsonConvert.SerializeObject(cashBoxConfiguration));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStandardLoggers(LogLevel.Debug);

            if (config.Package == "fiskaltrust.Middleware.SCU.AT.InMemory")
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

        private static ftCashBoxConfiguration GetDemoConfiguration()
        {
            var cashBoxConfiguration = new ftCashBoxConfiguration(Guid.NewGuid())
            {
                ftSignaturCreationDevices = new PackageConfiguration[] {
                    new PackageConfiguration
                    {
                        Url = new string[] {
                            "grpc://localhost:1401"
                        },
                        Id = Guid.Parse("1fc3b59f-9566-4d05-bd61-d5e1fdb5bdb8"),
                        Package = "fiskaltrust.Middleware.SCU.AT.InMemory",
                        Version = "2.0.0-rc1",
                        Configuration = new Dictionary<string, object>()
                    }
                }
            };
            return cashBoxConfiguration;
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
