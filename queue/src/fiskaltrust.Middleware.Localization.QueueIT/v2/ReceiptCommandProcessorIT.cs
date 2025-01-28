using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class ReceiptCommandProcessorIT
    {
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly IJournalITRepository _journalITRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;

        public ReceiptCommandProcessorIT(IITSSCDProvider itSSCDProvider, IJournalITRepository journalITRepository, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _itSSCDProvider = itSSCDProvider;
            _journalITRepository = journalITRepository;
            _queueItemRepository = queueItemRepository;
        }

        public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
        {
            var receiptCase = (request.ReceiptRequest.ftReceiptCase & 0xFFFF);
            if (receiptCase == (int) ReceiptCases.UnknownReceipt0x0000)
                return await UnknownReceipt0x0000Async(request);

            if (receiptCase == (int) ReceiptCases.PointOfSaleReceipt0x0001)
                return await PointOfSaleReceipt0x0001Async(request);

            if (receiptCase == (int) ReceiptCases.PaymentTransfer0x0002)
                return await PaymentTransfer0x0002Async(request);

            if (receiptCase == (int) ReceiptCases.PointOfSaleReceiptWithoutObligation0x0003)
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);

            if (receiptCase == (int) ReceiptCases.ECommerce0x0004)
                return await ECommerce0x0004Async(request);

            if (receiptCase == (int) ReceiptCases.Protocol0x0005)
                return await Protocol0x0005Async(request);

            request.ReceiptResponse.SetReceiptResponseError($"The given ftReceiptCase 0x{request.ReceiptRequest.ftReceiptCase:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

        public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
        {
            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;
            if (request.ReceiptRequest.IsVoid() || request.ReceiptRequest.IsRefund())
            {
                receiptResponse = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(_queueItemRepository, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
                if (receiptResponse.HasFailed())
                {
                    return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
                }
            }

            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse,
            });
            if (result.ReceiptResponse.HasFailed())
            {
                return new ProcessCommandResponse(result.ReceiptResponse, new List<ftActionJournal>());
            }

            var documentNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber);
            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber);
            if (!result.ReceiptResponse.ftReceiptIdentification.EndsWith("#"))
            {
                receiptResponse.ftReceiptIdentification = result.ReceiptResponse.ftReceiptIdentification;
            }
            else
            {
                receiptResponse.ftReceiptIdentification += $"{zNumber.Data.PadLeft(4, '0')}-{documentNumber.Data.PadLeft(4, '0')}";
            }

            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
            receiptResponse.InsertSignatureItems(SignaturItemFactory.CreatePOSReceiptFormatSignatures(receiptResponse));
            var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
            {
                ftReceiptCase = receiptRequest.ftReceiptCase,
                ReceiptNumber = long.Parse(documentNumber.Data),
                ZRepNumber = long.Parse(zNumber.Data)
            });
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
            return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
        }

        public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

        public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);
    }
}
