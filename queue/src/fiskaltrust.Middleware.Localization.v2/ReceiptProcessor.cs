using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2;

public class ReceiptProcessor : IReceiptProcessor
{
    private readonly ILifecycleCommandProcessor _lifecyclCommandProcessor;
    private readonly IReceiptCommandProcessor _receiptCommandProcessor;
    private readonly IDailyOperationsCommandProcessor _dailyOperationsCommandProcessor;
    private readonly IInvoiceCommandProcessor _invoiceCommandProcessor;
    private readonly IProtocolCommandProcessor _protocolCommandProcessor;
    private readonly ILogger<ReceiptProcessor> _logger;

    public ReceiptProcessor(ILogger<ReceiptProcessor> logger, ILifecycleCommandProcessor lifecyclCommandProcessor, IReceiptCommandProcessor receiptCommandProcessor, IDailyOperationsCommandProcessor dailyOperationsCommandProcessor, IInvoiceCommandProcessor invoiceCommandProcessor, IProtocolCommandProcessor protocolCommandProcessor)
    {
        _lifecyclCommandProcessor = lifecyclCommandProcessor;
        _receiptCommandProcessor = receiptCommandProcessor;
        _dailyOperationsCommandProcessor = dailyOperationsCommandProcessor;
        _invoiceCommandProcessor = invoiceCommandProcessor;
        _protocolCommandProcessor = protocolCommandProcessor;
        _logger = logger;
    }

    public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ReceiptResponse receiptResponse, ftQueue queue, ftQueueItem queueItem)
    {
        var processCommandRequest = new ProcessCommandRequest(queue, request, receiptResponse);
        ProcessCommandResponse? processCommandResponse = null;
        var receiptCase = request.ftReceiptCase.Case();

        try
        {
            if (request.ftReceiptCase.IsType(ReceiptCaseType.DailyOperations))
            {
                processCommandResponse = receiptCase switch
                {
                    ReceiptCase.ZeroReceipt0x2000 => await _dailyOperationsCommandProcessor.ZeroReceipt0x2000Async(processCommandRequest),
                    ReceiptCase.OneReceipt0x2001 => await _dailyOperationsCommandProcessor.OneReceipt0x2001Async(processCommandRequest),
                    ReceiptCase.ShiftClosing0x2010 => await _dailyOperationsCommandProcessor.ShiftClosing0x2010Async(processCommandRequest),
                    ReceiptCase.DailyClosing0x2011 => await _dailyOperationsCommandProcessor.DailyClosing0x2011Async(processCommandRequest),
                    ReceiptCase.MonthlyClosing0x2012 => await _dailyOperationsCommandProcessor.MonthlyClosing0x2012Async(processCommandRequest),
                    ReceiptCase.YearlyClosing0x2013 => await _dailyOperationsCommandProcessor.YearlyClosing0x2013Async(processCommandRequest),
                    _ => null
                };
            }

            if (request.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle))
            {
                processCommandResponse = receiptCase switch
                {
                    ReceiptCase.InitialOperationReceipt0x4001 => await _lifecyclCommandProcessor.InitialOperationReceipt0x4001Async(processCommandRequest),
                    ReceiptCase.OutOfOperationReceipt0x4002 => await _lifecyclCommandProcessor.OutOfOperationReceipt0x4002Async(processCommandRequest),
                    ReceiptCase.InitSCUSwitch0x4011 => await _lifecyclCommandProcessor.InitSCUSwitch0x4011Async(processCommandRequest),
                    ReceiptCase.FinishSCUSwitch0x4012 => await _lifecyclCommandProcessor.FinishSCUSwitch0x4012Async(processCommandRequest),
                    _ => null
                };
            }

            if (request.ftReceiptCase.IsType(ReceiptCaseType.Receipt))
            {
                processCommandResponse = receiptCase switch
                {
                    ReceiptCase.UnknownReceipt0x0000 => await _receiptCommandProcessor.UnknownReceipt0x0000Async(processCommandRequest).ConfigureAwait(false),
                    ReceiptCase.PointOfSaleReceipt0x0001 => await _receiptCommandProcessor.PointOfSaleReceipt0x0001Async(processCommandRequest),
                    ReceiptCase.PaymentTransfer0x0002 => await _receiptCommandProcessor.PaymentTransfer0x0002Async(processCommandRequest),
                    ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => await _receiptCommandProcessor.PointOfSaleReceiptWithoutObligation0x0003Async(processCommandRequest),
                    ReceiptCase.ECommerce0x0004 => await _receiptCommandProcessor.ECommerce0x0004Async(processCommandRequest),
                    ReceiptCase.DeliveryNote0x0005 => await _receiptCommandProcessor.DeliveryNote0x0005Async(processCommandRequest),
                    _ => null
                };
            }

            if (request.ftReceiptCase.IsType(ReceiptCaseType.Log))
            {
                processCommandResponse = receiptCase switch
                {
                    ReceiptCase.ProtocolUnspecified0x3000 => await _protocolCommandProcessor.ProtocolUnspecified0x3000Async(processCommandRequest),
                    ReceiptCase.ProtocolTechnicalEvent0x3001 => await _protocolCommandProcessor.ProtocolTechnicalEvent0x3001Async(processCommandRequest),
                    ReceiptCase.ProtocolAccountingEvent0x3002 => await _protocolCommandProcessor.ProtocolAccountingEvent0x3002Async(processCommandRequest),
                    ReceiptCase.InternalUsageMaterialConsumption0x3003 => await _protocolCommandProcessor.InternalUsageMaterialConsumption0x3003Async(processCommandRequest),
                    ReceiptCase.Order0x3004 => await _protocolCommandProcessor.Order0x3004Async(processCommandRequest),
                    ReceiptCase.Pay0x3005 => await _protocolCommandProcessor.Pay0x3005Async(processCommandRequest),
                    ReceiptCase.CopyReceiptPrintExistingReceipt0x3010 => await _protocolCommandProcessor.CopyReceiptPrintExistingReceipt0x3010Async(processCommandRequest),
                    _ => null
                };
            }

            if (request.ftReceiptCase.IsType(ReceiptCaseType.Invoice))
            {
                processCommandResponse = receiptCase switch
                {
                    ReceiptCase.InvoiceUnknown0x1000 => await _invoiceCommandProcessor.InvoiceUnknown0x1000Async(processCommandRequest),
                    ReceiptCase.InvoiceB2C0x1001 => await _invoiceCommandProcessor.InvoiceB2C0x1001Async(processCommandRequest),
                    ReceiptCase.InvoiceB2B0x1002 => await _invoiceCommandProcessor.InvoiceB2B0x1002Async(processCommandRequest),
                    ReceiptCase.InvoiceB2G0x1003 => await _invoiceCommandProcessor.InvoiceB2G0x1003Async(processCommandRequest),
                    _ => null
                }
            ;
            }

            if (processCommandResponse is not null)
            {
                return (processCommandResponse.receiptResponse, processCommandResponse.actionJournals);
            }

            receiptResponse.SetReceiptResponseError($"The given ftReceiptCase 0x{request.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return (receiptResponse, new List<ftActionJournal>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process receiptcase 0x{receiptcase}", request.ftReceiptCase.ToString("X"));
            receiptResponse.SetReceiptResponseError($"Failed to process receiptcase 0x{request.ftReceiptCase.ToString("X")}. with the following exception message: " + ex.Message);
            return (receiptResponse, new List<ftActionJournal>());
        }
    }
}
