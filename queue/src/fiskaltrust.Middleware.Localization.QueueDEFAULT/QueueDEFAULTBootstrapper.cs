using System;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
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
    // Class responsible for bootstrapping and configuring the DEFAULT queue.
    public class QueueDEFAULTBootstrapper : ILocalizedQueueBootstrapper
    {
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        public QueueDEFAULTBootstrapper(MiddlewareConfiguration middlewareConfiguration)
        {
            _middlewareConfiguration = middlewareConfiguration;
        }

        // Method to configure and register services that the application will use.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = QueueDEFAULTConfiguration.FromMiddlewareConfiguration(_middlewareConfiguration);
            // Check if the sandbox mode is disabled, and throw an exception if so.
            if (!config.Sandbox)
            {
                throw new InvalidOperationException("Only sandbox mode is allowed in this context.");
            }
            // Register services and components with the DI container. Services will be available to the application via dependency injection.
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorDEFAULT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorDEFAULT>()
                .AddScoped<SignatureItemFactoryDEFAULT>()
                .AddScoped<ICountrySpecificQueueRepository, CountrySpecificQueueRepository>()
                .AddScoped<ICountrySpecificSettings, CountrySpecificSettings>()
                .AddSingleton(sp => config)
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}