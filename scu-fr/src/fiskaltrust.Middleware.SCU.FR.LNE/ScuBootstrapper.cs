using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.FR.LNE;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }

    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(LneConfiguration.FromConfiguration(Configuration));
        // Singleton on purpose: the SCU holds the loaded private key for the lifetime of the queue.
        services.AddSingleton<IFRSSCD, LneFRSSCD>();
    }
}
