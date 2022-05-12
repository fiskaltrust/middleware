﻿using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureReceiptCommands(this IServiceCollection services)
        {
            services.AddSingleton<CashDepositReceiptCommand>();
            services.AddSingleton<CashWithdrawlReceiptCommand>();
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
