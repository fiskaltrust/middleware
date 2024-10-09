using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class LifecyclCommandProcessorPT : ILifecyclCommandProcessor
{
#pragma warning disable 
    private readonly IConfigurationRepository _configurationRepository;

    public LifecyclCommandProcessorPT(IConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
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
        var (queue, receiptRequest, receiptResponse) = request;
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        queue.StartMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateInitialOperationSignature(queue));
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        queue.StopMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(queue);
        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateOutOfOperationSignature(queue));
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}