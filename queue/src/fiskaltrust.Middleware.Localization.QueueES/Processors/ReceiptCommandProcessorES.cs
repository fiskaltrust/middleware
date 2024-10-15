﻿using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class ReceiptCommandProcessorES(IESSSCD sscd, ftQueueES queueES, ftSignaturCreationUnitES signaturCreationUnitES) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IESSSCD _sscd = sscd;
    private readonly ftQueueES _queueES = queueES;
    private readonly ftSignaturCreationUnitES _signaturCreationUnitES = signaturCreationUnitES;
#pragma warning restore

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case (int) ReceiptCases.UnknownReceipt0x0000:
                return await UnknownReceipt0x0000Async(request);
            case (int) ReceiptCases.PointOfSaleReceipt0x0001:
                return await PointOfSaleReceipt0x0001Async(request);
            case (int) ReceiptCases.PaymentTransfer0x0002:
                return await PaymentTransfer0x0002Async(request);
            case (int) ReceiptCases.PointOfSaleReceiptWithoutObligation0x0003:
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);
            case (int) ReceiptCases.ECommerce0x0004:
                return await ECommerce0x0004Async(request);
            case (int) ReceiptCases.Protocol0x0005:
                return await Protocol0x0005Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase(request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}