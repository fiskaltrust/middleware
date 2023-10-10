﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly ReceiptCommandProcessorIT _receiptCommandProcessorIT;
        private readonly DailyOperationsCommandProcessorIT _dailyOperationsCommandProcessorIT;
        private readonly InvoiceCommandProcessorIT _invoiceCommandProcessorIT;
        private readonly ProtocolCommandProcessorIT _protocolCommandProcessorIT;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly ILogger<SignProcessorIT> _logger;

        public SignProcessorIT(ILogger<SignProcessorIT> logger, IConfigurationRepository configurationRepository, ReceiptCommandProcessorIT receiptCommandProcessorIT, DailyOperationsCommandProcessorIT dailyOperationsCommandProcessorIT, InvoiceCommandProcessorIT invoiceCommandProcessorIT, ProtocolCommandProcessorIT protocolCommandProcessorIT, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _configurationRepository = configurationRepository;
            _receiptCommandProcessorIT = receiptCommandProcessorIT;
            _dailyOperationsCommandProcessorIT = dailyOperationsCommandProcessorIT;
            _invoiceCommandProcessorIT = invoiceCommandProcessorIT;
            _protocolCommandProcessorIT = protocolCommandProcessorIT;
            _queueItemRepository = queueItemRepository;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ReceiptResponse receiptResponse, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            if (request.IsDailyOperation())
            {
                try
                {
                    (var response, var actionJournals) = await _dailyOperationsCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process {receiptcase}.", request.ftReceiptCase);
                    receiptResponse.SetReceiptResponseErrored("Failed to process ZeroReceipt with the following exception message: " + ex.Message);
                    return (receiptResponse, new List<ftActionJournal>());
                }
            }

            if (request.IsVoid() || request.IsRefund())
            {
                await LoadReceiptReferencesToResponse(request, queueItem, receiptResponse);
            }

            try
            {
                if (request.IsReceiptOperation())
                {
                    var (response, actionJournals) = await _receiptCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsProtcolOperation())
                {
                    var (response, actionJournals) = await _protocolCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                if (request.IsInvoiceOperation())
                {
                    var (response, actionJournals) = await _invoiceCommandProcessorIT.ProcessReceiptAsync(new ProcessCommandRequest(queue, queueIT, request, receiptResponse, queueItem)).ConfigureAwait(false);
                    return (response, actionJournals);
                }

                receiptResponse.SetReceiptResponseErrored($"The given ReceiptCase 0x{request.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
                return (receiptResponse, new List<ftActionJournal>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process request");
                receiptResponse.SetReceiptResponseErrored("Failed to process receipt with the following exception message: " + ex.Message);
                return (receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}
