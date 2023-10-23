using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class QueueFRBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IArchiveProcessor, ArchiveProcessor>();
            services.AddScoped<ISignatureFactoryFR, SignatureFactoryFR>();
            services.AddScoped<RequestCommandFactory>();
            services.AddScoped<IMarketSpecificJournalProcessor, JournalProcessorFR>();
            services.AddScoped<IMarketSpecificSignProcessor, SignProcessorFR>();

            services.AddScoped<PaymentProveCommand>();
            services.AddScoped<InvoiceCommand>();
            services.AddScoped<ShiftReceiptCommand>();
            services.AddScoped<DailyReceiptCommand>();
            services.AddScoped<MonthlyReceiptCommand>();
            services.AddScoped<YearlyReceiptCommand>();
            services.AddScoped<BillCommand>();
            services.AddScoped<DeliveryNoteCommand>();
            services.AddScoped<CashPayInCommand>();
            services.AddScoped<CashPayOutCommand>();
            services.AddScoped<PaymentTransferCommand>();
            services.AddScoped<InternalMaterialCommand>();
            services.AddScoped<ForeignSalesCommand>();
            services.AddScoped<ZeroReceiptCommand>();
            services.AddScoped<StartReceiptCommand>();
            services.AddScoped<StopReceiptCommand>();
            services.AddScoped<ProtocolTechnicalEventLogCommand>();
            services.AddScoped<ProtocolAccountingAuditCommand>();
            services.AddScoped<ProtocolCustomCommand>();
            services.AddScoped<ArchiveCommand>();
            services.AddScoped<CopyCommand>();
            services.AddScoped<TicketCommand>();
        }
    }
}
