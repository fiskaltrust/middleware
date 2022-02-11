using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = GetConfiguration();
            services.AddSingleton(configuration);
            services.AddSingleton<ISerialCommunicationQueue>(x =>
            {
                var configuration = x.GetRequiredService<DieboldNixdorfConfiguration>();
                
                if (!string.IsNullOrEmpty(configuration.Url))
                {
                    var uri = new Uri(configuration.Url);
                    return new TcpCommunicationQueue(x.GetRequiredService<ILogger<TcpCommunicationQueue>>(), uri.Host, uri.Port);
                }
                else
                {
                    return new SerialPortCommunicationQueue(x.GetRequiredService<ILogger<SerialPortCommunicationQueue>>(), configuration.ComPort, 
                        configuration.ReadTimeoutMs, configuration.WriteTimeoutMs, configuration.EnableDtr);
                }
            });
            services.AddSingleton(x => new TseCommunicationCommandHelper(x.GetRequiredService<ILogger<TseCommunicationCommandHelper>>(), x.GetRequiredService<ISerialCommunicationQueue>(), configuration.SlotNumber));
            services.AddSingleton<AuthenticationTseCommandProvider>();
            services.AddSingleton<MaintenanceTseCommandProvider>();
            services.AddSingleton<UtilityTseCommandsProvider>();
            services.AddSingleton<StandardTseCommandsProvider>();
            services.AddSingleton<ExportTseCommandsProvider>();
            services.AddSingleton<TransactionTseCommandsProvider>();
            services.AddSingleton<BackgroundSCUTasks>();
            services.AddScoped<IDESSCD, DieboldNixdorfSCU>();
        }

        private DieboldNixdorfConfiguration GetConfiguration()
        {
            var configuration = JsonConvert.DeserializeObject<DieboldNixdorfConfiguration>(JsonConvert.SerializeObject(Configuration));
            if (configuration.SlotNumber == 0)
            {
                configuration.SlotNumber = 1;
            }
            if (string.IsNullOrEmpty(configuration.AdminUser))
            {
                configuration.AdminUser = "1";
            }
            if (string.IsNullOrEmpty(configuration.AdminPin))
            {
                configuration.AdminPin = "12345";
            }
            if (string.IsNullOrEmpty(configuration.TimeAdminUser))
            {
                configuration.TimeAdminUser = "2";
            }
            if (string.IsNullOrEmpty(configuration.TimeAdminPin))
            {
                configuration.TimeAdminPin = "12345";
            }
            return configuration;
        }
    }
}
