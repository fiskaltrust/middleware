using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            services.AddSingleton<InitialOperationReceiptCommand>();
            services.AddSingleton<OutOfOperationReceiptCommand>();
            services.AddSingleton<PosReceiptCommand>();
            return services;
        }
    }
}
