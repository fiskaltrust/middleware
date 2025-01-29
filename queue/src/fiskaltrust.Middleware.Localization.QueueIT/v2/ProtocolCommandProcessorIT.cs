using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class ProtocolCommandProcessorIT
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IJournalITRepository _journalITRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly ILogger<ProtocolCommandProcessorIT> _logger;

        public ProtocolCommandProcessorIT(IITSSCDProvider itSSCDProvider, IJournalITRepository journalITRepository, IMiddlewareQueueItemRepository queueItemRepository, ILogger<ProtocolCommandProcessorIT> logger)
        {
            _itSSCDProvider = itSSCDProvider;
            _journalITRepository = journalITRepository;
            _queueItemRepository = queueItemRepository;
            _logger = logger;
        }

        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int) ReceiptCases.ProtocolUnspecified0x3000)
                return await ProtocolUnspecified0x3000Async(request);

            if (receiptCase == (int) ReceiptCases.ProtocolTechnicalEvent0x3001)
                return await ProtocolTechnicalEvent0x3001Async(request);

            if (receiptCase == (int) ReceiptCases.ProtocolAccountingEvent0x3002)
                return await ProtocolAccountingEvent0x3002Async(request);

            if (receiptCase == (int) ReceiptCases.InternalUsageMaterialConsumption0x3003)
                return await InternalUsageMaterialConsumption0x3003Async(request);

            if (receiptCase == (int) ReceiptCases.Order0x3004)
                return await Order0x3004Async(request);

            if (receiptCase == (int) ReceiptCases.CopyReceiptPrintExistingReceipt0x3010)
                return await CopyReceiptPrintExistingReceipt0x3010Async(request);

            request.ReceiptResponse.SetReceiptResponseError($"The given ftReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request)
        {
            if ((request.ReceiptRequest.ftReceiptCase & 0x0000_0002_0000_0000) != 0)
            {
                var (queue, queueIT, receiptRequest, receiptResponse, queueItem) = request;
                try
                {
                    var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
                    {
                        ReceiptRequest = receiptRequest,
                        ReceiptResponse = receiptResponse
                    });
                    if (result.ReceiptResponse.HasFailed())
                    {
                        return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
                    }
                    return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
                }
                catch (Exception ex)
                {
                    receiptResponse.SetReceiptResponseError(ex.Message);
                    return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
                }
            }
            else
            {
                return await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
            }
        }

        public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            receiptResponse = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(_queueItemRepository, receiptRequest, queueItem, receiptResponse);
            if (receiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }
  
            try
            {
                var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = receiptRequest,
                    ReceiptResponse = receiptResponse
                });
                if (result.ReceiptResponse.HasFailed())
                {
                    return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
                }
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }
            catch (Exception ex)
            {
                receiptResponse.SetReceiptResponseError(ex.Message);
                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}