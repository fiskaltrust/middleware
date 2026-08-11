using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(PosNetConfiguration.FromConfiguration(Configuration));
        // Singleton on purpose: the SCU owns the persistent TCP connection to the printer and
        // serializes commands on it — one instance per configured device.
        services.AddSingleton<IPLSSCD, PosNetPLSSCD>();
    }
}
