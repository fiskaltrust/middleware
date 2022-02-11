using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            services.AddSingleton<DailyClosingReceiptCommand>();
            services.AddSingleton<DeltaTransactionReceiptCommand>();
            services.AddSingleton<DisabledQueueReceiptCommand>();
            services.AddSingleton<DisabledScuReceiptCommand>();
            services.AddSingleton<FailTransactionReceiptCommand>();
            services.AddSingleton<FinishScuSwitchReceiptCommand>();
            services.AddSingleton<InitialOperationReceiptCommand>();
            services.AddSingleton<InitiateScuSwitchReceiptCommand>();
            services.AddSingleton<MonthlyClosingReceiptCommand>();
            services.AddSingleton<OrderReceiptCommand>();
            services.AddSingleton<OtherReceiptCommand>();
            services.AddSingleton<OutOfOperationReceiptCommand>();
            services.AddSingleton<PosReceiptCommand>();
            services.AddSingleton<StartTransactionReceiptCommand>();
            services.AddSingleton<UpdateTransactionReceiptCommand>();
            services.AddSingleton<UsedFailedReceiptCommand>();
            services.AddSingleton<YearlyClosingReceiptCommand>();
            services.AddSingleton<ZeroReceiptCommand>();
            services.AddSingleton<HandwrittenReceiptCommand>();
            services.AddSingleton<SSCDFailedReceiptCommand>();
            
            return services;
        }
    }
}
