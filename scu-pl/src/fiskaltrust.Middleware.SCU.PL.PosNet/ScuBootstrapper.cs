using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(PosNetConfiguration.FromConfiguration(Configuration));
        // Singletons on purpose: the SCU owns the persistent connection to the printer and
        // serializes commands on it — one instance per configured device. The transport is a
        // registration of its own so the wire can be swapped or decorated (a recording transport
        // in the acceptance tests) without touching the SCU; which wire — TCP to the network
        // interface, serial to the USB/COM interface — follows from the DeviceUrl.
        services.AddSingleton(provider => PosNetTransportFactory.Create(provider.GetRequiredService<PosNetConfiguration>()));
        services.AddSingleton<PosNetClient>();
        services.AddSingleton<IPLSSCD, PosNetPLSSCD>();
    }
}
