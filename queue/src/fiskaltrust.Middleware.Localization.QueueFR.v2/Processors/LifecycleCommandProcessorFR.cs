using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Opens and closes the queue and switches its SCU. All four receipts are themselves grand-total
/// entries on this branch, so they are signed into the "G" chain (the switch receipts move to the
/// technical chain with ADR 0002, middleware#825): the chain has to start with the initial-operation receipt and end
/// with the out-of-operation receipt for an audit to be able to follow it end to end.
/// </summary>
/// <remarks>
/// Activation and deactivation are persisted only once the corresponding entry is signed. The
/// signing pipeline reports an unreachable SCU as an error response rather than throwing, so
/// acting on it unconditionally would leave the queue operational without a signed opening entry,
/// or permanently closed without a signed final one - in both cases with no way to retry.
/// </remarks>
public class LifecycleCommandProcessorFR : ILifecycleCommandProcessor
{
    private readonly IFRSSCD _sscd;
    private readonly FRSigningPipeline _pipeline;
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider;

    public LifecycleCommandProcessorFR(IFRSSCD sscd, FRSigningPipeline pipeline, ILocalizedQueueStorageProvider localizedQueueStorageProvider)
    {
        _sscd = sscd;
        _pipeline = pipeline;
        _localizedQueueStorageProvider = localizedQueueStorageProvider;
    }

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;

        var info = await _sscd.GetInfoAsync().ConfigureAwait(false);
        if (!FRSSCDInfoHelper.HasSignatureCreationData(info))
        {
            receiptResponse.SetReceiptResponseError("The configured SCU reports no usable signature creation data. A French queue may not be started without a certificate and private key, because every receipt it issues has to be signed.");
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateInitialOperationSignature(queue));

        var response = await _pipeline.SignAsync(request, [actionJournal]).ConfigureAwait(false);
        if (!FRSigningPipeline.Succeeded(response))
        {
            return new ProcessCommandResponse(response.receiptResponse, []);
        }

        await _localizedQueueStorageProvider.ActivateQueueAsync().ConfigureAwait(false);
        return response;
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;

        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(receiptRequest, receiptResponse);
        receiptResponse.AddSignatureItem(SignaturItemFactory.CreateOutOfOperationSignature(queue));

        var response = await _pipeline.SignAsync(request, [actionJournal]).ConfigureAwait(false);
        if (!FRSigningPipeline.Succeeded(response))
        {
            // Leaving the queue open is what makes the closing retryable; a deactivated queue
            // rejects every request, including the retry of this receipt.
            return new ProcessCommandResponse(response.receiptResponse, []);
        }

        await _localizedQueueStorageProvider.DeactivateQueueAsync().ConfigureAwait(false);
        response.receiptResponse.MarkAsDisabled();
        return response;
    }

    /// <summary>
    /// The outgoing SCU signs its last entry. From here on the signing pipeline refuses every
    /// receipt but 0x4012 until the incoming SCU is configured (the queue is restarted with it) and
    /// confirms the switch, so no receipt can be signed by an SCU the switch has not recorded.
    /// </summary>
    /// <remarks>
    /// The switch neither closes nor reopens the queue and leaves the periods open (ADR 0005): the
    /// archive is split by certificate serial, not by a closing. Whether a daily closing has to
    /// precede the switch is still open on market-fr#177 and is not enforced.
    /// </remarks>
    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request)
    {
        var (_, receiptRequest, receiptResponse) = request;

        if (await _pipeline.Chains.GetPendingScuSwitchAsync().ConfigureAwait(false) is { } pending)
        {
            receiptResponse.SetReceiptResponseError($"An SCU switch away from {pending.Outgoing} is already pending. Send the finish-SCU-switch receipt (0x4012) from the incoming SCU first.");
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var scuSwitch = new FRScuSwitch { Outgoing = FRScuIdentity.From(await _sscd.GetInfoAsync().ConfigureAwait(false)) };
        RecordScuSwitch(receiptResponse, scuSwitch);

        var actionJournal = ftActionJournalFactory.CreateInitScuSwitchActionJournal(receiptRequest, receiptResponse, scuSwitch);
        var response = await _pipeline.SignAsync(request, [actionJournal]).ConfigureAwait(false);
        return FRSigningPipeline.Succeeded(response) ? response : new ProcessCommandResponse(response.receiptResponse, []);
    }

    /// <summary>
    /// The incoming SCU signs its first entry, chained on the outgoing SCU's last signature
    /// (ADR 0005), and the queue accepts receipts again.
    /// </summary>
    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request)
    {
        var (_, receiptRequest, receiptResponse) = request;

        var pending = await _pipeline.Chains.GetPendingScuSwitchAsync().ConfigureAwait(false);
        if (pending is null)
        {
            receiptResponse.SetReceiptResponseError("No SCU switch is pending. Send the init-SCU-switch receipt (0x4011) from the outgoing SCU first.");
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var info = await _sscd.GetInfoAsync().ConfigureAwait(false);
        if (!FRSSCDInfoHelper.HasSignatureCreationData(info))
        {
            receiptResponse.SetReceiptResponseError("The incoming SCU reports no usable signature creation data. The switch can only be finished by an SCU that can sign.");
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var incoming = FRScuIdentity.From(info);
        if (incoming.SameAs(pending.Outgoing))
        {
            receiptResponse.SetReceiptResponseError($"The configured SCU is still the outgoing one ({pending.Outgoing}). Configure the incoming SCU, restart the queue and send the finish-SCU-switch receipt (0x4012) again.");
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var scuSwitch = new FRScuSwitch { Outgoing = pending.Outgoing, Incoming = incoming };
        RecordScuSwitch(receiptResponse, scuSwitch);

        var actionJournal = ftActionJournalFactory.CreateFinishScuSwitchActionJournal(receiptRequest, receiptResponse, scuSwitch);
        var response = await _pipeline.SignAsync(request, [actionJournal]).ConfigureAwait(false);
        return FRSigningPipeline.Succeeded(response) ? response : new ProcessCommandResponse(response.receiptResponse, []);
    }

    private static void RecordScuSwitch(ReceiptResponse receiptResponse, FRScuSwitch scuSwitch)
    {
        var stateData = MiddlewareStateData.FromReceiptResponse(receiptResponse);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.ScuSwitch = scuSwitch;
        receiptResponse.ftStateData = stateData;
    }
}
