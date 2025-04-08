using System;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueAT.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueAT
{
    public class QueueATBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(sp => QueueATConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()));

            services.AddScoped<IATSSCDProvider, ATSSCDProvider>();
            services.AddScoped<IRequestCommandFactory, RequestCommandFactory>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IRequestCommandFactory, RequestCommandFactory>();

            services.AddScoped<IMarketSpecificJournalProcessor, JournalProcessorAT>();
            services.AddScoped<IMarketSpecificSignProcessor, SignProcessorAT>();

            services.AddScoped<DisabledQueueReceiptCommand>();
            services.AddScoped<InitialOperationReceiptCommand>();
            services.AddScoped<MonthlyClosingReceiptCommand>();
            services.AddScoped<OutOfOperationReceiptCommand>();
            services.AddScoped<PosReceiptCommand>();
            services.AddScoped<YearlyClosingReceiptCommand>();
            services.AddScoped<ZeroReceiptCommand>();
        }
    }
}
