using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                .AddSingleton(x => new CustomRTServerCommunicationQueue(Id, x.GetRequiredService<CustomRTServerClient>(), x.GetRequiredService<ILogger<CustomRTServerCommunicationQueue>>(), x.GetRequiredService<CustomRTServerConfiguration>()))
                .AddScoped<CustomRTServerClient>()
                .AddScoped<IITSSCD>(x => new CustomRTServerSCU(Id, x.GetRequiredService<ILogger<CustomRTServerSCU>>(), x.GetRequiredService<CustomRTServerConfiguration>(), x.GetRequiredService<CustomRTServerClient>(), x.GetRequiredService<CustomRTServerCommunicationQueue>()));
        }
    }
}