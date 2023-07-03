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
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class QueueDEBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<ITransactionPayloadFactory, DSFinVKTransactionPayloadFactory>()
                .AddScoped<SignatureFactoryDE>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorDE>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorDE>()
                .AddScoped<IMasterDataService, MasterDataService>()
                .AddSingleton(sp => QueueDEConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<ILogger<QueueDEConfiguration>>(), sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IDESSCDProvider>(sp =>
                {
                    var sscdProvider = new DESSCDProvider(
                        sp.GetRequiredService<ILogger<DESSCDProvider>>(),
                        sp.GetRequiredService<IClientFactory<IDESSCD>>(),
                        sp.GetRequiredService<IConfigurationRepository>(),
                        sp.GetRequiredService<MiddlewareConfiguration>(),
                        sp.GetRequiredService<QueueDEConfiguration>());

                    sscdProvider.RegisterCurrentScuAsync().Wait();

                    return sscdProvider;
                })
                .AddSingleton<ITarFileCleanupService>(sp =>
                {
                    var tarFileCleanupService = new TarFileCleanupService(
                        sp.GetRequiredService<ILogger<TarFileCleanupService>>(),
                        sp.GetRequiredService<IMiddlewareJournalDERepository>(),
                        sp.GetRequiredService<MiddlewareConfiguration>(),
                        sp.GetRequiredService<QueueDEConfiguration>());

                    tarFileCleanupService.CleanupAllTarFilesAsync().Wait();

                    return tarFileCleanupService;
                })
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}
