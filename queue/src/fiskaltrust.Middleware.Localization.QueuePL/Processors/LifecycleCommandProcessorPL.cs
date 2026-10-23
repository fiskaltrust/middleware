using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.QueuePL.Factories;
using fiskaltrust.Middleware.Localization.QueuePL.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// Registers/deregisters the queue. There is no device-fiscalization command: fiscalizing a Polish
/// register is a certified-technician (serwis) act, so the initial-operation receipt only verifies
/// via GetInfo that the connected register reports itself as fiscalized. In sandbox a register that
/// reports itself as not fiscalized is accepted too, so integrations can be tested on demo hardware;
/// a register whose state cannot be read is refused either way.
/// </summary>
public class LifecycleCommandProcessorPL(IPLSSCD sscd, ILocalizedQueueStorageProvider localizedQueueStorageProvider, bool isSandbox = false) : ILifecycleCommandProcessor
{
    private readonly IPLSSCD _sscd = sscd;
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider = localizedQueueStorageProvider;
    private readonly bool _isSandbox = isSandbox;

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;

        PLSSCDInfo info;
        try
        {
            info = await _sscd.GetInfoAsync();
        }
        catch (Exception ex)
        {
            // GetInfoAsync has no response to carry an error state, so it still reports failure by
            // throwing: the register (or the SCU) could not be reached, rejected the status command,
            // or answered undecodably. Every one of them means the fiscalization state is unknown,
            // and it reaches the caller as a receipt response with an action journal, not as a raw
            // exception. Caught broadly on purpose: an unverified register must not activate a queue,
            // and reading the status changes nothing on the register, so retrying is always safe.
            var unverified = $"The initial-operation receipt could not verify the register's fiscalization state. Retry once the register is reachable. Details: {ex.Message}";
            receiptResponse.SetReceiptResponseError(unverified);
            return new ProcessCommandResponse(receiptResponse, [ftActionJournalFactory.CreateWrongStateForInitialOperationActionJournal(
                queue, receiptRequest, receiptResponse, unverified)]);
        }

        var acceptedInSandbox = _isSandbox && PLSSCDInfoHelper.IsNonFiscal(info);
        if (!PLSSCDInfoHelper.IsFiscalized(info) && !acceptedInSandbox)
        {
            var message = $"The connected register does not report FiscalizationState 'Fiscalized'. A Polish register must be fiscalized by an authorized serwis before the queue can start operating.";
            receiptResponse.SetReceiptResponseError(message);
            return new ProcessCommandResponse(receiptResponse, [ftActionJournalFactory.CreateWrongStateForInitialOperationActionJournal(queue, receiptRequest, receiptResponse, message)]);
        }

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

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);
}
