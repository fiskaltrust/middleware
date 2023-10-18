using fiskaltrust.Middleware.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueME.Services;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Factories;
using fiskaltrust.Middleware.Contracts.Interfaces;
using System.Threading.Tasks;
using System;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class QueueMeBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorME>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorME>()
                .AddSingleton(sp => QueueMEConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IMESSCDProvider, MESSCDProvider>()
                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .AddSingleton<SignatureItemFactory>()
                .ConfigureReceiptCommands();
        }

        public Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services)
        {
            ConfigureServices(services);
            return Task.FromResult<Func<IServiceProvider, Task>>((IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<IMESSCDProvider>().RegisterCurrentScuAsync());
        }
    }
}
