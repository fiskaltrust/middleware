using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = null!;

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var configuration = JsonConvert.DeserializeObject<CustomRTServerConfiguration>(JsonConvert.SerializeObject(Configuration));

            _ = serviceCollection
                .AddSingleton(configuration)
                .AddSingleton<CustomRTServerCommunicationQueue>()
                .AddScoped<CustomRTServerClient>()
                .AddScoped<IITSSCD, CustomRTServerSCU>();
        }
    }
}