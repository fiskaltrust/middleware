using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class LifecycleCommandProcessorPT(ILocalizedQueueStorageProvider localizedQueueStorageProvider) : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider = localizedQueueStorageProvider;

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var actionJournal = ftActionJournalFactoryPT.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        await _localizedQueueStorageProvider.ActivateQueueAsync();
        receiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreateInitialOperationSignature(queue));
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        await _localizedQueueStorageProvider.DeactivateQueueAsync();
        var actionJournal = ftActionJournalFactoryPT.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreateOutOfOperationSignature(queue));
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);
}