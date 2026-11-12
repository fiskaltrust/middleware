using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.FR.SelfCertified;

public class ScuBootstrapper : IMiddlewareBootstrapper
{
    public Guid Id { get; set; }

    public Dictionary<string, object> Configuration { get; set; } = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = SelfCertifiedConfiguration.FromConfiguration(Configuration);
        services.AddSingleton(configuration);
        // Singleton on purpose: the SCU holds the loaded private key for the lifetime of the queue.
        services.AddSingleton<IFRSSCD>(provider =>
        {
            if (!string.IsNullOrWhiteSpace(configuration.ElectronicAddress))
            {
                provider.GetService<ILogger<SelfCertifiedFRSSCD>>()?.LogWarning(FRIssuerClaims.ElectronicAddressIgnored("self-certified"));
            }
            return new SelfCertifiedFRSSCD(configuration);
        });
    }
}
