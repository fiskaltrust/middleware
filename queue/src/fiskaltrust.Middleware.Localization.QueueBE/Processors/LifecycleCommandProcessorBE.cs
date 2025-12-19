using fiskaltrust.Middleware.Localization.QueueBE.Factories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class LifecycleCommandProcessorBE(ILocalizedQueueStorageProvider localizedQueueStorageProvider) : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider = localizedQueueStorageProvider;

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        await _localizedQueueStorageProvider.ActivateQueueAsync();
        receiptResponse.AddSignatureItem(SignatureItemFactory.CreateInitialOperationSignature(queue));
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        await _localizedQueueStorageProvider.DeactivateQueueAsync();
        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignatureItemFactory.CreateOutOfOperationSignature(queue));
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);
}