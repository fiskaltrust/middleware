using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly LifecycleCommandProcessorIT _lifecyclCommandProcessorIT;
        private readonly ReceiptCommandProcessorIT _receiptCommandProcessorIT;
        private readonly DailyOperationsCommandProcessorIT _dailyOperationsCommandProcessorIT;
        private readonly InvoiceCommandProcessorIT _invoiceCommandProcessorIT;
        private readonly ProtocolCommandProcessorIT _protocolCommandProcessorIT;
        private readonly ILogger<SignProcessorIT> _logger;

        public SignProcessorIT(ILogger<SignProcessorIT> logger, IConfigurationRepository configurationRepository, LifecycleCommandProcessorIT lifecyclCommandProcessorIT, ReceiptCommandProcessorIT receiptCommandProcessorIT, DailyOperationsCommandProcessorIT dailyOperationsCommandProcessorIT, InvoiceCommandProcessorIT invoiceCommandProcessorIT, ProtocolCommandProcessorIT protocolCommandProcessorIT)
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
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
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

        public async Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue) => (await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false)).CashBoxIdentification;
    }
}
