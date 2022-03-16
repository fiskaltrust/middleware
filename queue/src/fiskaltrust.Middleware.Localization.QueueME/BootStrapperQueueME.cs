using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands.Factories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class QueueMEBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<SignatureFactoryME>()
                .AddScoped<IMarketSpecificSignProcessor, SignProcessorME>()
                .AddScoped<JournalProcessorME>()
                .AddSingleton(sp => QueueMEConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))

                .AddSingleton<IRequestCommandFactory, RequestCommandFactory>()
                .ConfigureReceiptCommands();
        }
    }
}
