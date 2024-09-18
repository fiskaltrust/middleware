using System;
using System.IO;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.Middleware.SCU.DE.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Test.Launcher
{
    public static class Program
    {
        private static readonly bool useHelipad = false;
        private static readonly string cashBoxId = "";
        private static readonly string accessToken = "";
        private static readonly string fccDirectory = "";

        private static readonly string configurationFilePath = "";
        private static readonly string serviceFolder = Directory.GetCurrentDirectory();

        public static void Main()
        {
            ftCashBoxConfiguration cashBoxConfiguration;
            if (useHelipad)
            {
                cashBoxConfiguration = HelipadHelper.GetConfigurationAsync(cashBoxId, accessToken).Result;
                if (!string.IsNullOrEmpty(fccDirectory) && cashBoxConfiguration.ftSignaturCreationDevices[0].Configuration.ContainsKey("FccId"))
                {
                    if (cashBoxConfiguration.ftSignaturCreationDevices[0].Configuration.ContainsKey("FccDirectory"))
                    {
                        cashBoxConfiguration.ftSignaturCreationDevices[0].Configuration["FccDirectory"] = fccDirectory;
                    }
                    else
                    {
                        cashBoxConfiguration.ftSignaturCreationDevices[0].Configuration.Add("FccDirectory", fccDirectory);
                    }
                }
            }
            else if (string.IsNullOrEmpty(configurationFilePath))
            {
                cashBoxConfiguration = GetDemoConfiguration();
            }
            else
            {
                cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(File.ReadAllText(configurationFilePath));
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

            if (config.Package == "fiskaltrust.Middleware.SCU.DE.Swissbit")
            {
                ConfigureSwissbit(config, serviceCollection);
            }else if (config.Package == "fiskaltrust.Middleware.SCU.DE.SwissbitCloud")
            {
                ConfigureSwissbitCloud(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2")
            {
                ConfigureSwissbitCloudV2(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.SCU.DE.FiskalyCertified")
            {
                ConfigureFiskalyCertified(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.SCU.DE.DieboldNixdorf")
            {
                ConfigureDieboldNixdorf(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.SCU.DE.CryptoVision")
            {
                ConfigureCryptoVision(config, serviceCollection);
            }
            else if (config.Package == "fiskaltrust.Middleware.SCU.DE.InMemory")
            {
                ConfigureIMemory(config, serviceCollection);
            }
            else
            {
                throw new NotSupportedException($"The given package {config.Package} is not supported.");
            }
            var provider = serviceCollection.BuildServiceProvider();
            var desscd = provider.GetRequiredService<IDESSCD>();
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
                        Package = "fiskaltrust.Middleware.SCU.DE.Swissbit",
                        Version = "1.3.1-rc1",
                        Configuration = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "devicePath", "F:" }
                        }
                    }
                }
            };
            return cashBoxConfiguration;
        }

        private static ftCashBoxConfiguration GetCryptoVisionDemoConfiguration()
        {
            var cashBoxConfiguration = new ftCashBoxConfiguration(Guid.NewGuid())
            {
                ftSignaturCreationDevices = new PackageConfiguration[] {
                    new PackageConfiguration
                    {
                        Url = new string[] {
                            "grpc://localhost:15000",
                            "http://localhost:15001/fec8700a-2af8-4de9-acf0-fb73acc00b24",
                            "rest://localhost:15002/fec8700a-2af8-4de9-acf0-fb73acc00b24"
                        },
                        Id = Guid.Parse("fec8700a-2af8-4de9-acf0-fb73acc00b24"),
                        Package = "fiskaltrust.Middleware.SCU.DE.CryptoVision",
                        Version = "1.3.1-rc1",
                        Configuration = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "devicePath", "v:" },
                        }
                    }
                }
            };
            return cashBoxConfiguration;
        }

        private static ftCashBoxConfiguration GetSwissbitDemoConfiguration()
        {
            var cashBoxConfiguration = new ftCashBoxConfiguration(Guid.NewGuid())
            {
                ftSignaturCreationDevices = new PackageConfiguration[] {
                    new PackageConfiguration
                    {
                        Url = new string[] {
                            "grpc://localhost:15000",
                            "http://localhost:15001/fec8700a-2af8-4de9-acf0-fb73acc00b24",
                            "rest://localhost:15002/fec8700a-2af8-4de9-acf0-fb73acc00b24"
                        },
                        Id = Guid.Parse("fec8700a-2af8-4de9-acf0-fb73acc00b24"),
                        Package = "fiskaltrust.Middleware.SCU.DE.Swissbit",
                        Version = "1.3.1-rc1",
                        Configuration = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "devicePath", "s:" },
                        }
                    }
                }
            };
            return cashBoxConfiguration;
        }

        private static ftCashBoxConfiguration GetSwissbitCloudDemoConfiguration()
        {
            var cashBoxConfiguration = new ftCashBoxConfiguration(Guid.NewGuid())
            {
                ftSignaturCreationDevices = new PackageConfiguration[] {
                    new PackageConfiguration
                    {
                        Url = new string[] {
                            "grpc://localhost:18004"
                        },
                        Id = Guid.Parse("a9e2aebf-4d3a-40e3-88e2-2bde853e5221"),
                        Package = "fiskaltrust.Middleware.SCU.DE.SwissbitCloud",
                        Version = "1.3.21",
                        Configuration = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "FccDirectory", "C:\\ProgramData\\fiskaltrust\\FCC\\sfcc-ftde-6rdx-so0d" },
                            { "FccId", "" },
                            { "FccSecret", "" },
                            { "ErsCode", "" },
                            { "ActivationToken", "" },
                        }
                    }
                }
            };
            return cashBoxConfiguration;
        }

        private static void ConfigureSwissbit(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new Swissbit.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureSwissbitCloud(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new SwissbitCloud.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureSwissbitCloudV2(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            queue.Configuration.Add("TseSerialNumber", "fd79e44187bce2e2dcc886c89bf993df26d157503c4d953557b2e5af73571876");
            queue.Configuration.Add("TseAccessToken", "6945c6ab69f348cd3779b5ee139466c4");
            var bootStrapper = new SwissbitCloudV2.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureCryptoVision(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new CryptoVision.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureFiskalyCertified(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new FiskalyCertified.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
        }

        private static void ConfigureDieboldNixdorf(PackageConfiguration queue, ServiceCollection serviceCollection)
        {
            var bootStrapper = new DieboldNixdorf.ScuBootstrapper
            {
                Id = queue.Id,
                Configuration = queue.Configuration
            };
            bootStrapper.ConfigureServices(serviceCollection);
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
