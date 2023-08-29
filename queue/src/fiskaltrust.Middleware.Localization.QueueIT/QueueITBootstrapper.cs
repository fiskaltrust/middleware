using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorIT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorIT>()
                .AddScoped<ReceiptTypeProcessorFactory>()
                .AddSingleton(sp => QueueITConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IITSSCDProvider>(sp =>
                {
                    var sscdProvider = new ITSSCDProvider(
                        sp.GetRequiredService<IClientFactory<IITSSCD>>(),
                        sp.GetRequiredService<MiddlewareConfiguration>(),
                        sp.GetRequiredService<ILogger<ITSSCDProvider>>());
                    sscdProvider.RegisterCurrentScuAsync().Wait();
                    return sscdProvider;
                });
        }
    }
}
