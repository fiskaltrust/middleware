using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;
using ScuSignatureTypeFR = fiskaltrust.Middleware.SCU.FR.Abstraction.Cases.SignatureTypeFR;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The archive receipt (0x3011), as in v1.2 (workerFR <c>ArchiveRequest</c>, <c>GetArchivePayload</c>):
/// signed into the archive chain A against the previous archive receipt's signature, it closes the
/// day and the archive period, prints the day and archive-period totals and resets them
/// (market-fr#221, decided 2026-11-09). Before signing, the queue fixes the archive's boundaries
/// (<see cref="FRArchiveBoundaries"/>) and hands them to the SCU, which signs them.
/// </summary>
/// <remarks>
/// <para>
/// The archive range departs from v1.2 (decided 2026-11-10): it runs from the record right after the
/// previous archive receipt up to and including this archive receipt, so every record lies in exactly
/// one range and each archive carries the trace of its own creation. v1.2 ran from the previous
/// archive receipt (included) to the current one (excluded).
/// </para>
/// <para>
/// The receipt neither generates nor hashes the archive: it fixes which receipts the archive holds.
/// The archive keeps its own signature (ADR 0007), and its integrity rests on the signatures and
/// chains of the entries inside it.
/// </para>
/// </remarks>
public class FRArchiveReceipt
{
    private const long V1ReceiptSignatureType = 0x4652_0000_0000_0001;

    private readonly FRSigningPipeline _pipeline;
    private readonly FRChainStateProvider _chains;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItems;
    private readonly AsyncLazy<IMiddlewareReceiptJournalRepository> _receiptJournals;
    private readonly AsyncLazy<IMiddlewareActionJournalRepository> _actionJournals;

    public FRArchiveReceipt(FRSigningPipeline pipeline, FRChainStateProvider chains, AsyncLazy<IMiddlewareQueueItemRepository> queueItems,
        AsyncLazy<IMiddlewareReceiptJournalRepository> receiptJournals, AsyncLazy<IMiddlewareActionJournalRepository> actionJournals)
    {
        _pipeline = pipeline;
        _chains = chains;
        _queueItems = queueItems;
        _receiptJournals = receiptJournals;
        _actionJournals = actionJournals;
    }

    public async Task<ProcessCommandResponse?> SignAsync(ProcessCommandRequest request)
    {
        // As v1 (ArchiveCommand): the first receipt of a queue has nothing to archive.
        if (request.queue.ftReceiptNumerator == 0)
        {
            request.ReceiptResponse.SetReceiptResponseError("Nothing to archive: the first receipt of a queue cannot be an archive receipt.");
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var (previousArchiveQueueItemId, day, month, year) = await _chains.ReadAsync(queueFR =>
        {
            var totals = new FRPeriodTotalsAccumulator(queueFR);
            return (queueFR.ALastQueueItemId, totals.Snapshot(FRTotalsPeriod.Day).Current, totals.Snapshot(FRTotalsPeriod.Month).Current, totals.Snapshot(FRTotalsPeriod.Year).Current);
        }).ConfigureAwait(false);

        // As v1.2 (AddDailySignature), the archive receipt closes the day itself and prints its
        // totals; a training one prints the live figures and closes nothing.
        request.ReceiptResponse.AddSignatureItem(ScuSignatureTypeFR.DayTotals, "Day Totals", FRPeriodTotalsItems.Readable(day));

        // A training archive receipt archives nothing (v1.2 TrainingRequest).
        if (!request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            var stateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
            stateData.FR ??= new MiddlewareStateDataFR();
            stateData.FR.Archive = await BoundariesAsync(request, previousArchiveQueueItemId, day, month, year).ConfigureAwait(false);
            request.ReceiptResponse.ftStateData = stateData;
        }

        return await _pipeline.SignAsync(request).ConfigureAwait(false);
    }

    private async Task<FRArchiveBoundaries> BoundariesAsync(ProcessCommandRequest request, Guid? previousArchiveQueueItemId, FRTotals day, FRTotals month, FRTotals year)
    {
        var queueItems = await _queueItems;
        var response = request.ReceiptResponse;

        var first = await FirstContainedAsync(queueItems, previousArchiveQueueItemId).ConfigureAwait(false);

        return new FRArchiveBoundaries
        {
            PreviousArchiveQueueItemId = previousArchiveQueueItemId,
            FirstContainedReceiptQueueItemId = first?.ftQueueItemId ?? response.ftQueueItemID,
            FirstContainedReceiptMoment = first is null ? response.ftReceiptMoment : MomentOf(first, response),
            // The archive receipt is its own last contained receipt (decided 2026-11-10).
            LastContainedReceiptQueueItemId = response.ftQueueItemID,
            LastContainedReceiptMoment = response.ftReceiptMoment,
            // The archive receipt's own receipt journal and action journal rows are written after it
            // is signed, so these name the last rows before them.
            LastReceiptJournalId = (await (await _receiptJournals).GetWithLastTimestampAsync().ConfigureAwait(false))?.ftReceiptJournalId,
            LastActionJournalId = (await (await _actionJournals).GetWithLastTimestampAsync().ConfigureAwait(false))?.ftActionJournalId,
            LastJournalFRId = null,
            Day = day,
            Month = month,
            Year = year,
        };
    }

    /// <summary>
    /// The first record of the archive: the queue item right after the previous archive receipt, or the
    /// queue's first queue item. After a half migration, a previous archive receipt that v1.2 signed
    /// belongs to no v1.2 archive (v1.2 put it in the next one), so the v2 range starts on it.
    /// </summary>
    private static async Task<ftQueueItem?> FirstContainedAsync(IMiddlewareQueueItemRepository queueItems, Guid? previousArchiveQueueItemId)
    {
        if (previousArchiveQueueItemId is not { } previousId)
        {
            return await queueItems.GetEntriesOnOrAfterTimeStampAsync(0, 1).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        var previous = await queueItems.GetAsync(previousId).ConfigureAwait(false);
        if (previous is null)
        {
            return null;
        }

        if (SignedByV12(previous))
        {
            return previous;
        }

        return await queueItems.GetEntriesOnOrAfterTimeStampAsync(previous.TimeStamp + 1, 1).FirstOrDefaultAsync().ConfigureAwait(false);
    }

    /// <summary>A v2 archive receipt keeps its boundaries in its state data; one v1.2 signed has none, only its v1 receipt signature.</summary>
    private static bool SignedByV12(ftQueueItem archive)
    {
        if (string.IsNullOrEmpty(archive.response))
        {
            return false;
        }

        var response = JsonSerializer.Deserialize<ReceiptResponse>(archive.response);
        return response is not null
            && FRStateData.Archive(response) is null
            && response.ftSignatures?.Any(x => (long) x.ftSignatureType == V1ReceiptSignatureType) == true;
    }

    /// <summary>The moment the receipt of a queue item was issued; the archive receipt itself has no stored response yet.</summary>
    private static DateTime? MomentOf(ftQueueItem item, ReceiptResponse current)
    {
        if (item.ftQueueItemId == current.ftQueueItemID)
        {
            return current.ftReceiptMoment;
        }

        if (string.IsNullOrEmpty(item.response))
        {
            return null;
        }

        using var document = JsonDocument.Parse(item.response);
        return document.RootElement.TryGetProperty("ftReceiptMoment", out var moment) && moment.TryGetDateTime(out var value) ? value : null;
    }
}
