using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueuePT.v2;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueuePT
{
    public class QueuePTBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<SignProcessorPT>()
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorPT>()
                .AddScoped<ReceiptCommandProcessorPT>()
                .AddScoped<ProtocolCommandProcessorPT>()
                .AddScoped<LifecyclCommandProcessorPT>()
                .AddScoped<InvoiceCommandProcessorPT>()
                .AddScoped<DailyOperationsCommandProcessorPT>()
                .AddSingleton(sp => QueuePTConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()));
        }
    }
}
