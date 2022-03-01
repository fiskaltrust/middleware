﻿using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class QueueDEBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ITransactionPayloadFactory, DSFinVKTransactionPayloadFactory>();
            services.AddScoped<SignatureFactoryDE>();
            services.AddScoped<IMarketSpecificSignProcessor, SignProcessorDE>();
            services.AddScoped<JournalProcessorDE>();
            services.AddScoped<IMasterDataService, MasterDataService>();
            services.AddSingleton<IDESSCDProvider>(sp =>
            {
                var sscdProvider = new DESSCDProvider(sp.GetRequiredService<ILogger<DESSCDProvider>>(), sp.GetRequiredService<IClientFactory<IDESSCD>>(),
                    sp.GetRequiredService<IConfigurationRepository>(), sp.GetRequiredService<MiddlewareConfiguration>());
                sscdProvider.RegisterCurrentScuAsync().Wait();

                return sscdProvider;
            });
            services.AddSingleton<ITarFileCleanupService>(sp =>
            {
                var tarFileCleanupService = new TarFileCleanupService(
                    sp.GetRequiredService<ILogger<TarFileCleanupService>>(),
                    sp.GetRequiredService<IMiddlewareJournalDERepository>(),
                    sp.GetRequiredService<MiddlewareConfiguration>());

                tarFileCleanupService.CleanupAllTarFilesAsync().Wait();

                return tarFileCleanupService;
            });

            services.AddSingleton<IRequestCommandFactory, RequestCommandFactory>();
            services.ConfigureReceiptCommands();
        }
    }
}
