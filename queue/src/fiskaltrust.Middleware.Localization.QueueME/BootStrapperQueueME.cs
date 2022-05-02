using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueME.Services;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class QueueMEBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<SignatureFactoryME>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorME>()
                .AddScoped<IJournalProcessor, JournalProcessorME>()
                .AddSingleton(sp => QueueMEConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IMESSCDProvider>(sp =>
                {
                    var sscdProvider = new MESSCDProvider(
                        sp.GetRequiredService<IClientFactory<IMESSCD>>(),
                        sp.GetRequiredService<MiddlewareConfiguration>());
                    sscdProvider.RegisterCurrentScuAsync().Wait();

                    return sscdProvider;
                })
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}
