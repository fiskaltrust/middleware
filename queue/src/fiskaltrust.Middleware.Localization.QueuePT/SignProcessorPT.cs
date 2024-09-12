using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Interface.Models;
using fiskaltrust.Middleware.Localization.QueuePT.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT
{
    public class SignProcessorPT
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly LifecyclCommandProcessorPT _lifecyclCommandProcessorIT;
        private readonly ReceiptCommandProcessorPT _receiptCommandProcessorIT;
        private readonly DailyOperationsCommandProcessorPT _dailyOperationsCommandProcessorIT;
        private readonly InvoiceCommandProcessorPT _invoiceCommandProcessorIT;
        private readonly ProtocolCommandProcessorPT _protocolCommandProcessorIT;
        private readonly ILogger<SignProcessorPT> _logger;

        public SignProcessorPT(ILogger<SignProcessorPT> logger, IConfigurationRepository configurationRepository, LifecyclCommandProcessorPT lifecyclCommandProcessorIT, ReceiptCommandProcessorPT receiptCommandProcessorIT, DailyOperationsCommandProcessorPT dailyOperationsCommandProcessorIT, InvoiceCommandProcessorPT invoiceCommandProcessorIT, ProtocolCommandProcessorPT protocolCommandProcessorIT)
        {
            _configurationRepository = configurationRepository;
            _lifecyclCommandProcessorIT = lifecyclCommandProcessorIT;
            _receiptCommandProcessorIT = receiptCommandProcessorIT;
            _dailyOperationsCommandProcessorIT = dailyOperationsCommandProcessorIT;
            _invoiceCommandProcessorIT = invoiceCommandProcessorIT;
            _protocolCommandProcessorIT = protocolCommandProcessorIT;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ReceiptResponse receiptResponse, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = new ftQueuePT
            {
                CashBoxIdentification = Guid.NewGuid().ToString(),
            };
            receiptResponse.ftCashBoxIdentification = queueIT.CashBoxIdentification;

            try
            {

                if (request.IsDailyOperation())
                {
                    (var response, var actionJournals) = await _dailyOperationsCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsLifeCycleOperation())
                {
                    (var response, var actionJournals) = await _lifecyclCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsReceiptOperation())
                {
                    var (response, actionJournals) = await _receiptCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsProtocolOperation())
                {
                    var (response, actionJournals) = await _protocolCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsInvoiceOperation())
                {
                    var (response, actionJournals) = await _invoiceCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
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
}
