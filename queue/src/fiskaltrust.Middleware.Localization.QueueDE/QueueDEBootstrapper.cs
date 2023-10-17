using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Interfaces;
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
                .AddSingleton<IDESSCDProvider, DESSCDProvider>()
                .AddSingleton<ITarFileCleanupService, TarFileCleanupService>()
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }

        public Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services)
        {
            ConfigureServices(services);
            return Task.FromResult<Func<IServiceProvider, Task>>(async (IServiceProvider services) =>
            {
                await services.GetRequiredService<IDESSCDProvider>().RegisterCurrentScuAsync();
                await services.GetRequiredService<ITarFileCleanupService>().CleanupAllTarFilesAsync();
            });
        }
    }
}
