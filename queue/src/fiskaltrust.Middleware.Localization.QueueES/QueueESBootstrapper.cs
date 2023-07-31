using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Services;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueES.Constants;
using fiskaltrust.Middleware.Localization.QueueES.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Extensions;
using fiskaltrust.Middleware.Localization.QueueES.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class QueueESBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<ISSCD, SscdES>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorES>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorES>()
                .AddScoped<SignatureItemFactoryES>()
                .AddScoped<ICountrySpecificQueueRepository, CountrySpecificQueueRepository>()
                .AddScoped<ICountrySpecificSettings, CountrySpecificSettings>()
                .AddSingleton(sp => QueueESConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IESSSCDProvider>(sp =>
                {
                    var sscdProvider = new ESSSCDProvider(null,
                        sp.GetRequiredService<MiddlewareConfiguration>());
                    //sscdProvider.RegisterCurrentScuAsync().Wait();
                    return sscdProvider;
                })
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}