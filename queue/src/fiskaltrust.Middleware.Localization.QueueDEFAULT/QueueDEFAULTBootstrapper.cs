﻿using System;
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
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        public QueueDEFAULTBootstrapper(MiddlewareConfiguration middlewareConfiguration)
        {
            _middlewareConfiguration = middlewareConfiguration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = QueueDEFAULTConfiguration.FromMiddlewareConfiguration(_middlewareConfiguration);

            if (!config.Sandbox)
            {
                throw new InvalidOperationException("Only sandbox mode is allowed in this context.");
            }
        
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