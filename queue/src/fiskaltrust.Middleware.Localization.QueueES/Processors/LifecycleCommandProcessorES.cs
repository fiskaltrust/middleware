﻿using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class LifecycleCommandProcessorES : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _queueStorageProvider;
    private readonly IESSSCD _sscd;

    public LifecycleCommandProcessorES(IESSSCD sscd, ILocalizedQueueStorageProvider queueStorageProvider)
    {
        _sscd = sscd;
        _queueStorageProvider = queueStorageProvider;
    }

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case (int) ReceiptCases.InitialOperationReceipt0x4001:
                return await InitialOperationReceipt0x4001Async(request);
            case (int) ReceiptCases.OutOfOperationReceipt0x4002:
                return await OutOfOperationReceipt0x4002Async(request);
            case (int) ReceiptCases.InitSCUSwitch0x4011:
                return await InitSCUSwitch0x4011Async(request);
            case (int) ReceiptCases.FinishSCUSwitch0x4012:
                return await FinishSCUSwitch0x4012Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase(request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        // should an initial operation receipt initialize both the Alta and Anulacion chains?
        // maybe by cancelling its self? ^^

        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = null,
            PreviousReceiptResponse = null,
        });
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(request.ReceiptRequest, request.ReceiptResponse);
        await _queueStorageProvider.ActivateQueueAsync();

        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateInitialOperationSignature(request.queue));
        return new ProcessCommandResponse(response.ReceiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        await _queueStorageProvider.DeactivateQueueAsync();

        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateOutOfOperationSignature(queue));
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
