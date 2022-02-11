using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = JsonConvert.DeserializeObject<FiskalySCUConfiguration>(JsonConvert.SerializeObject(Configuration));
            services.AddSingleton(config);
            services.AddScoped<IFiskalyApiProvider, FiskalyV2ApiProvider>();
            services.AddScoped<ClientCache>();
            services.AddScoped<IDESSCD, FiskalySCU>();
        }
    }
}
