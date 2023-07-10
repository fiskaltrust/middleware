using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueES
{
    public class QueueESBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                    .AddScoped<IMarketSpecificSignProcessor, SignProcessorES>()
                    .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorES>();
        }
    }
}
