using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.ES.VerifactuSCU;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(VeriFactuSCUConfiguration.FromConfiguration(Configuration));
        services.AddScoped<IESSSCD, VeriFactuSCU>();
    }
}
