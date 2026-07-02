using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = null!;

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var configuration = JsonConvert.DeserializeObject<EpsonRTServerConfiguration>(JsonConvert.SerializeObject(Configuration))!;

            _ = serviceCollection
                .AddSingleton(configuration)
                .AddScoped<IEpsonRTServerClient, EpsonRTServerClient>()
                .AddSingleton(x => new EpsonRTServerCommunicationQueue(Id, x.GetRequiredService<IEpsonRTServerClient>(), x.GetRequiredService<ILogger<EpsonRTServerCommunicationQueue>>(), x.GetRequiredService<EpsonRTServerConfiguration>()))
                .AddScoped<IITSSCD>(x => new EpsonRTServerSCU(Id, x.GetRequiredService<ILogger<EpsonRTServerSCU>>(), x.GetRequiredService<EpsonRTServerConfiguration>(), x.GetRequiredService<IEpsonRTServerClient>(), x.GetRequiredService<EpsonRTServerCommunicationQueue>()));
        }
    }
}
