using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.v2.QueueES.Storage;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class LifecycleCommandProcessorES : ILifecycleCommandProcessor
{
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider;
    private readonly ISCUStateProvider _scuStateProvider;
    private readonly IESSSCD _sscd;

    public LifecycleCommandProcessorES(IESSSCD sscd, ILocalizedQueueStorageProvider localizedQueueStorageProvider, ISCUStateProvider scuStateProvider)
    {
        _sscd = sscd;
        _localizedQueueStorageProvider = localizedQueueStorageProvider;
        _scuStateProvider = scuStateProvider;
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

        var (queue, receiptRequest, receiptResponse) = request;
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
            StateData = await _scuStateProvider.LoadAsync()
        });
        await _scuStateProvider.SaveAsync(response.StateData); // what happens if the storage is down?
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        await _localizedQueueStorageProvider.ActivateQueueAsync();

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

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
