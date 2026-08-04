using fiskaltrust.Middleware.Localization.QueueGR.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class LifecycleCommandProcessorGR(ILocalizedQueueStorageProvider localizedQueueStorageProvider, AsyncLazy<IConfigurationRepository> configurationRepository) : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider = localizedQueueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        await _localizedQueueStorageProvider.ActivateQueueAsync();

        var configurationRepository = await _configurationRepository;
        var queueGR = await configurationRepository.GetQueueGRAsync(queue.ftQueueId);
        if (string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
            await configurationRepository.InsertOrUpdateQueueGRAsync(queueGR);
        }

        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateInitialOperationSignature(queue));
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        await _localizedQueueStorageProvider.DeactivateQueueAsync();
        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateOutOfOperationSignature(queue));
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);
}