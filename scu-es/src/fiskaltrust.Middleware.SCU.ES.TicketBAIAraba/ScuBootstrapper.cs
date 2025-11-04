using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TicketBaiSCUConfiguration.FromConfiguration(Configuration));
        services.AddScoped<IESSSCD, TicketBaiArabaSCU>();
    }
}
