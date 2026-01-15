using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class LifecycleCommandProcessorES(ILocalizedQueueStorageProvider queueStorageProvider, AsyncLazy<IConfigurationRepository> configurationRepository) : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _queueStorageProvider = queueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        await _queueStorageProvider.ActivateQueueAsync();
        var queueES = await (await _configurationRepository).GetQueueESAsync(request.queue.ftQueueId);
        if (string.IsNullOrEmpty(queueES.InvoiceSeries))
        {
            queueES.InvoiceSeries = $"fkt{Helper.ShortGuid(request.queue.ftQueueId)}1000";
        }
        if (string.IsNullOrEmpty(queueES.SimplifiedInvoiceSeries))
        {
            queueES.SimplifiedInvoiceSeries = $"fkt{Helper.ShortGuid(request.queue.ftQueueId)}0000";
        }

        await (await _configurationRepository).InsertOrUpdateQueueESAsync(queueES);

        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateInitialOperationSignature(queue));
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
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
