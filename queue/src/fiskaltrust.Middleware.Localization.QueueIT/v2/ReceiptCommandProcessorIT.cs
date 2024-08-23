using System.Collections.Generic;
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
            if (request.ReceiptRequest.IsVoid() || request.ReceiptRequest.IsRefund())
            {
                await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
            }

            var (queue, queueIt, receiptRequest, receiptResponse, queueItem) = request;

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
            if (!receiptResponse.ftReceiptIdentification.EndsWith("#"))
            {
                // in case we do not have the local identification we will add it based on what we get back 
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

        private async Task LoadReceiptReferencesToResponse(ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            var queueItems = _queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
            // What should we do in this case? Cannot really proceed with the storno but we
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
