using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Opens and closes the queue. Both receipts are themselves grand-total entries, so they are
/// signed into the "G" chain: the chain has to start with the initial-operation receipt and end
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

    public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);
}
