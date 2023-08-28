using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<ISSCD, SscdIT>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorIT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorIT>()
                .AddScoped<SignatureItemFactoryIT>()
                .AddScoped<ReceiptTypeProcessorFactory>()
                .AddScoped<ICountrySpecificQueueRepository,CountrySpecificQueueRepository>()
                .AddScoped<ICountrySpecificSettings, CountrySpecificSettings>()
                .AddSingleton(sp => QueueITConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IITSSCDProvider>(sp =>
                {
                    var sscdProvider = new ITSSCDProvider(
                        sp.GetRequiredService<IClientFactory<IITSSCD>>(),
                        sp.GetRequiredService<MiddlewareConfiguration>());
                    sscdProvider.RegisterCurrentScuAsync().Wait();
                    return sscdProvider;
                })
                .ConfigureReceiptCommands();
        }
    }
}
