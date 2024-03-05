using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<SignProcessorIT>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessor>(x => new SignProcessor(x.GetRequiredService<IConfigurationRepository>(), x.GetRequiredService<SignProcessorIT>()))
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorIT>()
                .AddScoped<ReceiptCommandProcessorIT>()
                .AddScoped<ProtocolCommandProcessorIT>()
                .AddScoped<LifecyclCommandProcessorIT>()
                .AddScoped<InvoiceCommandProcessorIT>()
                .AddScoped<DailyOperationsCommandProcessorIT>()
                .AddSingleton(sp => QueueITConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton<IITSSCDProvider, ITSSCDProvider>();
        }
    }
}
