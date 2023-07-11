using System;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Constants;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands.Factories;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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
                .AddScoped<ICountrySpecificQueueRepository, CountrySpecificQueueRepository>()
                .AddScoped<ICountrySpecificSettings, CountrySpecificSettings>()
                .AddSingleton(sp =>
                {
                    var middlewareConfiguration = sp.GetRequiredService<MiddlewareConfiguration>();
                    var config = JsonConvert.DeserializeObject<QueueDEFAULTConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));

                    if (!config.Sandbox)
                    {
                        throw new InvalidOperationException("Only sandbox mode is allowed in this context.");
                    }

                    return config;
                })
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}
