using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var scuMeConfig = JsonConvert.DeserializeObject<ScuMEConfiguration>(JsonConvert.SerializeObject(Configuration));

            _ = serviceCollection
                .AddSingleton(scuMeConfig)
                .AddScoped<IMESSCD, FiscalizationServiceSCU>();
        }
    }
}