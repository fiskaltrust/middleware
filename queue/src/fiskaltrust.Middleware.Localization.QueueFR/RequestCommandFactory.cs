using System;
using fiskaltrust.Middleware.Localization.QueueFR.Constants;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class RequestCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCommandFactory(IServiceProvider serviceCollection) => _serviceProvider = serviceCollection;

        public RequestCommand Create(long ftReceiptCase) => (ftReceiptCase & 0xFFFF) switch
        {
            ((long) ReceiptCaseFR.PaymentProve & 0xFFFF) => _serviceProvider.GetRequiredService<PaymentProveCommand>(),
            ((long) ReceiptCaseFR.Invoice & 0xFFFF) => _serviceProvider.GetRequiredService<InvoiceCommand>(),
            ((long) ReceiptCaseFR.ShiftReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<ShiftReceiptCommand>(),
            ((long) ReceiptCaseFR.DailyReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<DailyReceiptCommand>(),
            ((long) ReceiptCaseFR.MonthlyReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<MonthlyReceiptCommand>(),
            ((long) ReceiptCaseFR.YearlyReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<YearlyReceiptCommand>(),
            ((long) ReceiptCaseFR.BillReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<BillCommand>(),
            ((long) ReceiptCaseFR.DeliveryNote & 0xFFFF) => _serviceProvider.GetRequiredService<DeliveryNoteCommand>(),
            ((long) ReceiptCaseFR.CashPayIn & 0xFFFF) => _serviceProvider.GetRequiredService<CashPayInCommand>(),
            ((long) ReceiptCaseFR.CashPayout & 0xFFFF) => _serviceProvider.GetRequiredService<CashPayOutCommand>(),
            ((long) ReceiptCaseFR.PaymentTransfer & 0xFFFF) => _serviceProvider.GetRequiredService<PaymentTransferCommand>(),
            ((long) ReceiptCaseFR.InternalMaterial & 0xFFFF) => _serviceProvider.GetRequiredService<InternalMaterialCommand>(),
            ((long) ReceiptCaseFR.ForeignSaleReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<ForeignSalesCommand>(),
            ((long) ReceiptCaseFR.ZeroReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<ZeroReceiptCommand>(),
            ((long) ReceiptCaseFR.StartReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<StartReceiptCommand>(),
            ((long) ReceiptCaseFR.StopReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<StopReceiptCommand>(),
            ((long) ReceiptCaseFR.ProtocolTechnicalEventLog & 0xFFFF) => _serviceProvider.GetRequiredService<ProtocolTechnicalEventLogCommand>(),
            ((long) ReceiptCaseFR.ProtocolAccountingAudit & 0xFFFF) => _serviceProvider.GetRequiredService<ProtocolAccountingAuditCommand>(),
            ((long) ReceiptCaseFR.ProtocolCustom & 0xFFFF) => _serviceProvider.GetRequiredService<ProtocolCustomCommand>(),
            ((long) ReceiptCaseFR.ArchiveReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<ArchiveCommand>(),
            ((long) ReceiptCaseFR.CopyReceipt & 0xFFFF) => _serviceProvider.GetRequiredService<CopyCommand>(),
            ((long) ReceiptCaseFR.Ticket & 0xFFFF) => _serviceProvider.GetRequiredService<TicketCommand>(),
            ((long) ReceiptCaseFR.Unknown & 0xFFFF) => _serviceProvider.GetRequiredService<TicketCommand>(),
            _ => _serviceProvider.GetRequiredService<TicketCommand>()
        };
    }
}
