using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
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

        public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
        {
            var (queue, queueIT, receiptRequest, receiptResponse, queueItem) = request;
            await LoadReceiptReferencesToResponse(receiptRequest, queueItem, receiptResponse);
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

        private async Task LoadReceiptReferencesToResponse(ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            var queueItems = _queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
            await foreach (var existingQueueItem in queueItems)
            {
                var referencedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(existingQueueItem.response);
                var documentNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber).Data;
                var zNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
                var documentMoment = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data;
                documentMoment ??= queueItem.cbReceiptMoment.ToString("yyyy-MM-dd");
                var signatures = new List<SignaturItem>();
                signatures.AddRange(receiptResponse.ftSignatures);
                signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = documentNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = documentMoment,
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        },
                    });
                receiptResponse.ftSignatures = signatures.ToArray();
                break;
            }
        }
    }
}