using System;
using System.Threading.Tasks;
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
            services.AddScoped<ExportService>();
            services.AddScoped<RequestCommandFactory>();

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

        public Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services)
        {
            ConfigureServices(services);
            return Task.FromResult<Func<IServiceProvider, Task>>(_ => Task.CompletedTask);
        }
    }
}
