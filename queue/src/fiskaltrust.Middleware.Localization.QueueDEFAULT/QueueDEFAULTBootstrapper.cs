using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Constants;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Services;
using fiskaltrust.Middleware.Localization.QueueIT;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class QueueDEFAULTBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorDEFAULT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorDEFAULT>()
                .AddScoped<SignatureItemFactoryDEFAULT>()
                .AddScoped<ICountrySpecificQueueRepository,CountrySpecificQueueRepository>()
                .AddScoped<ICountrySpecificSettings, CountrySpecificSettings>()
                .AddSingleton(sp => QueueDEFAULTConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IITSSCDProvider>(sp =>
                {
                    var sscdProvider = new ITSSCDProvider(
                        sp.GetRequiredService<IClientFactory<IITSSCD>>(),
                        sp.GetRequiredService<MiddlewareConfiguration>());
                    sscdProvider.RegisterCurrentScuAsync().Wait();
                    return sscdProvider;
                })
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}
