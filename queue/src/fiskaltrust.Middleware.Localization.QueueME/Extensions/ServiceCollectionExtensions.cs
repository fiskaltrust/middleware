using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            return services;
        }
    }
}
