using System;
using System.Threading.Tasks;
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

        public Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services)
        {
            ConfigureServices(services);
            return Task.FromResult<Func<IServiceProvider, Task>>(_ => Task.CompletedTask);
        }
    }
}
