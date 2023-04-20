using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Factories;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorIT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorIT>()
                .AddScoped<SignatureItemFactoryIT>()
                .AddSingleton(sp => QueueITConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
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
