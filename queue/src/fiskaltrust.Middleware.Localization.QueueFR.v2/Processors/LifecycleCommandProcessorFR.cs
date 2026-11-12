using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Opens and closes the queue. Both receipts are signed into the technical chain (ADR 0002): the
/// chain has to start with the initial-operation receipt and end with the out-of-operation
/// receipt for an audit to be able to follow it end to end.
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
    private readonly FRMigration? _migration;
    private readonly FRChainStateProvider? _chains;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository>? _queueItems;

    /// <param name="chains">With <paramref name="queueItems"/>, lets the out-of-operation receipt check it follows an archive receipt (Ref LNE 1.8 Ex. 7, 10). Required: the check never switches off.</param>
    public LifecycleCommandProcessorFR(IFRSSCD sscd, FRSigningPipeline pipeline, ILocalizedQueueStorageProvider localizedQueueStorageProvider,
        FRChainStateProvider chains, AsyncLazy<IMiddlewareQueueItemRepository> queueItems, FRMigration? migration = null)
        : this(sscd, pipeline, localizedQueueStorageProvider, migration)
    {
        _chains = chains ?? throw new ArgumentNullException(nameof(chains));
        _queueItems = queueItems ?? throw new ArgumentNullException(nameof(queueItems));
    }

    /// <summary>
    /// Tests only: without the chain state and the queue items, the out-of-operation receipt does not check that it follows an
    /// archive receipt. Production code uses the public constructor, which requires them.
    /// </summary>
    internal LifecycleCommandProcessorFR(IFRSSCD sscd, FRSigningPipeline pipeline, ILocalizedQueueStorageProvider localizedQueueStorageProvider, FRMigration? migration = null)
    {
        _migration = migration;
        _sscd = sscd;
        _pipeline = pipeline;
        _localizedQueueStorageProvider = localizedQueueStorageProvider;
    }

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        if (RejectTraining(receiptRequest, receiptResponse, "initial-operation"))
        {
            return new ProcessCommandResponse(receiptResponse, []);
        }

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
        if (RejectTraining(receiptRequest, receiptResponse, "out-of-operation"))
        {
            return new ProcessCommandResponse(receiptResponse, []);
        }

        if (await NotRightAfterAnArchiveReceiptAsync(receiptResponse.ftQueueItemID).ConfigureAwait(false) is { } refusal)
        {
            receiptResponse.SetReceiptResponseError(refusal);
            return new ProcessCommandResponse(receiptResponse, []);
        }

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
    /// Why the queue may not be taken out of operation now, or null (Ref LNE 1.8 Ex. 7, 10): as v1.2 archived then
    /// stopped, no record that adds to an archive may follow the non-training archive receipt the chain state names, so
    /// every such record lies in a signed archive range before the queue closes. Only JET events (chain L) and training
    /// documents (chain X) may follow it: a compliant POS sends the JET 20 its archive receipt asked for, and may answer
    /// other JET events on a zero receipt, before it stops. Those L and X entries, and the stop itself, lie outside every
    /// archive-receipt range; the portal's closing export, which runs to the last signed entry, covers them. Entries
    /// without a signature (refused, failed, or this receipt itself) do not count. The entries are read by queue row from
    /// the archive receipt on, never from the start.
    /// </summary>
    private async Task<string?> NotRightAfterAnArchiveReceiptAsync(Guid currentQueueItemId)
    {
        if (_chains is null || _queueItems is null)
        {
            // Only the tests-only constructor leaves them out.
            return null;
        }

        var archiveId = await _chains.ReadAsync(queueFR => queueFR.ALastQueueItemId).ConfigureAwait(false);
        var queueItems = await _queueItems;
        if (archiveId is not { } id || await queueItems.GetAsync(id).ConfigureAwait(false) is not { } archive)
        {
            return "The queue can be taken out of operation only right after an archive receipt (0x3011), and it has none: send an archive receipt first.";
        }

        await foreach (var item in queueItems.GetEntriesOnOrAfterTimeStampAsync(archive.TimeStamp).ConfigureAwait(false))
        {
            if (item.ftQueueRow <= archive.ftQueueRow || item.ftQueueItemId == currentQueueItemId)
            {
                continue;
            }

            Journal.FR.Archive.V2.ArchiveEntry? entry;
            try
            {
                entry = Journal.FR.Archive.V2.ArchiveEntry.Read(item.ftQueueItemId, item.ftQueueId, item.ftQueueRow, item.request, item.response);
            }
            catch (Journal.FR.Archive.V2.ArchiveEntryException ex)
            {
                return $"The queue can be taken out of operation only right after an archive receipt (0x3011); queue row {item.ftQueueRow} after it cannot be read: {ex.Message}";
            }

            if (entry is not null && entry.Chain is not ("L" or "X"))
            {
                return $"The queue can be taken out of operation only right after an archive receipt (0x3011), and {entry.ReceiptIdentification} was signed after it: send an archive receipt first.";
            }
        }

        return null;
    }

    /// <summary>
    /// A lifecycle receipt opens or closes the real queue, so it cannot be simulated: a training
    /// one would be signed into the training chain and still change the fiscal queue. v1.2 signed it
    /// as a training receipt without touching the queue; v2 refuses it before anything is signed.
    /// </summary>
    private static bool RejectTraining(ReceiptRequest request, ReceiptResponse response, string receipt)
    {
        if (!request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return false;
        }

        response.SetReceiptResponseError($"An {receipt} receipt cannot be sent in training mode: it opens or closes the fiscal queue. Send it without the training flag.");
        return true;
    }

    public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    /// <summary>Initiate migration of a v1.2 queue (market-fr ADR 0003): see <see cref="FRMigration"/>.</summary>
    public Task<ProcessCommandResponse?> InitiateMigration0x4021Async(ProcessCommandRequest request) => _migration?.InitiateAsync(request) ?? Task.FromResult<ProcessCommandResponse?>(null);

    /// <summary>Finish migration of a v1.2 queue (market-fr ADR 0003): see <see cref="FRMigration"/>.</summary>
    public Task<ProcessCommandResponse?> FinishMigration0x4022Async(ProcessCommandRequest request) => _migration?.FinishAsync(request) ?? Task.FromResult<ProcessCommandResponse?>(null);

    public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);
}
