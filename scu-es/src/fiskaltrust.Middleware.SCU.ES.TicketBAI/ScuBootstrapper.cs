using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(JsonConvert.DeserializeObject<TicketBaiSCUConfiguration>(JsonConvert.SerializeObject(Configuration)));
        services.AddScoped<TicketBaiRequestFactory>();
        services.AddScoped<IESSSCD, TicketBaiSCU>();
    }
}
