using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Extensions
{
    // Static class containing extension methods for configuring receipt commands for the DEFAULT queue.
    public static class ServiceCollectionExtensions
    {
        //"Receipt commands need to be registered here".
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            services.AddSingleton<DailyClosingReceiptCommand>();
            services.AddSingleton<InitialOperationReceiptCommand>();
            services.AddSingleton<MonthlyClosingReceiptCommand>();
            services.AddSingleton<OutOfOperationReceiptCommand>();
            services.AddSingleton<PosReceiptCommand>();
            services.AddSingleton<YearlyClosingReceiptCommand>();
            services.AddSingleton<ZeroReceiptCommand>();
            return services;
        }
    }
}
