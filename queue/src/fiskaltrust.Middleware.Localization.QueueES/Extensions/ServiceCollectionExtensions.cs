using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueES.RequestCommands;

namespace fiskaltrust.Middleware.Localization.QueueES.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            services.AddSingleton<DailyClosingReceiptCommand>();
            services.AddSingleton<InitialOperationReceiptCommand>();
            services.AddSingleton<MonthlyClosingReceiptCommand>();
            services.AddSingleton<OutOfOperationReceiptCommand>();
            services.AddSingleton<PosReceiptCommand>();
            services.AddSingleton<YearlyClosingReceiptCommand>();
            services.AddSingleton<ZeroReceiptCommandES>();
            return services;
        }
    }
}
