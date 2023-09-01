using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.Epson
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = null!;

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var epsonScuConfig = JsonConvert.DeserializeObject<EpsonScuConfiguration>(JsonConvert.SerializeObject(Configuration));

            _ = serviceCollection
                .AddSingleton(epsonScuConfig)
                .AddScoped<EpsonCommandFactory>()
                .AddScoped<IITSSCD, EpsonSCU>()
                .AddScoped<EpsonCommunicationClientV2>();
        }
    }
}