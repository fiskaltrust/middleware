﻿using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class ReceiptCommandProcessorES(IESSSCD sscd, Storage.ES.IConfigurationRepository configurationRepository, IReadOnlyQueueItemRepository queueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IESSSCD _sscd = sscd;
    private readonly Storage.ES.IConfigurationRepository _configurationRepository = configurationRepository;
    private readonly IReadOnlyQueueItemRepository _queueItemRepository = queueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase.Case();
        switch (receiptCase)
        {
            case ReceiptCase.UnknownReceipt0x0000:
                return await UnknownReceipt0x0000Async(request).ConfigureAwait(false);
            case ReceiptCase.PointOfSaleReceipt0x0001:
                return await PointOfSaleReceipt0x0001Async(request);
            case ReceiptCase.PaymentTransfer0x0002:
                return await PaymentTransfer0x0002Async(request);
            case ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003:
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);
            case ReceiptCase.ECommerce0x0004:
                return await ECommerce0x0004Async(request);
            case ReceiptCase.Protocol0x0005:
                return await Protocol0x0005Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase((long) request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var queueES = await _configurationRepository.GetQueueESAsync(request.queue.ftQueueId);
        var previousQueueItem = queueES.SSCDSignQueueItemId is not null ? await _queueItemRepository.GetAsync(queueES.SSCDSignQueueItemId.Value) : null;

        if (previousQueueItem is not null)
        {
            if (previousQueueItem?.request is null)
            {
                throw new Exception("Previous queue item request is null");
            }

            if (previousQueueItem?.request is null)
            {
                throw new Exception("Previous queue item request is null");
            }
        }

        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem.request)!, // handle null case?
            PreviousReceiptResponse = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem.response)!,
        });

        queueES.SSCDSignQueueItemId = response.ReceiptResponse.ftQueueItemID;
        await _configurationRepository.InsertOrUpdateQueueESAsync(queueES);

        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
