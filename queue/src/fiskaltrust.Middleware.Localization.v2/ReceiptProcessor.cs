using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
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
        try
        {

            if (request.IsDailyOperation())
            {
                (var response, var actionJournals) = await _dailyOperationsCommandProcessor.ProcessReceiptAsync(new ProcessCommandRequest(queue, request, receiptResponse)).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsLifeCycleOperation())
            {
                (var response, var actionJournals) = await _lifecyclCommandProcessor.ProcessReceiptAsync(new ProcessCommandRequest(queue, request, receiptResponse)).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsReceiptOperation())
            {
                var (response, actionJournals) = await _receiptCommandProcessor.ProcessReceiptAsync(new ProcessCommandRequest(queue, request, receiptResponse)).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsProtocolOperation())
            {
                var (response, actionJournals) = await _protocolCommandProcessor.ProcessReceiptAsync(new ProcessCommandRequest(queue, request, receiptResponse)).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsInvoiceOperation())
            {
                var (response, actionJournals) = await _invoiceCommandProcessor.ProcessReceiptAsync(new ProcessCommandRequest(queue, request, receiptResponse)).ConfigureAwait(false);
                return (response, actionJournals);
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
