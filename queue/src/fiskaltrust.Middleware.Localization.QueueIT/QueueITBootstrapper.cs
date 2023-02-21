using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
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
                .AddScoped<SignatureItemFactoryIT>();
        }
    }
}
