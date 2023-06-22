using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using fiskaltrust.Middleware.SCU.DE.Helpers.DisabledSCU;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = JsonConvert.DeserializeObject<DeutscheFiskalSCUConfiguration>(JsonConvert.SerializeObject(Configuration));
            services.AddSingleton(configuration);
            services.AddSingleton<IFccInitializationService, DeutscheFiskalFccInitializationService>();
            services.AddSingleton<IFccProcessHost, DeutscheFiskalFccProcessHost>();
            services.AddScoped<IFccDownloadService, DeutscheFiskalFccDownloadService>();
            services.AddScoped<FccAdminApiProvider>();
            services.AddScoped<FccErsApiProvider>();
            services.AddScoped<FirewallHelper>();
            if (!configuration.Disable)
            {
                services.AddScoped<IDESSCD, SwissbitCloudSCU>();
            }
            else
            {
                services.AddScoped<IDESSCD, DisabledSCU>();
            }
        }
    }
}
