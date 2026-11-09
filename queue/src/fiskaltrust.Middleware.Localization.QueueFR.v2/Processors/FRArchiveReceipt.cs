using System.Text;
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

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The archive receipt (0x3011), as in v1.2 (workerFR <c>ArchiveRequest</c>, <c>GetArchivePayload</c>):
/// signed into the archive chain A against the previous archive receipt's signature, it closes the
/// day and the archive period, prints the archive-period totals and resets them (market-fr#221,
/// decided 2026-11-09). Before signing, the queue fixes the archive's boundaries
/// (<see cref="FRArchiveBoundaries"/>) and hands them to the SCU, which signs them.
/// </summary>
/// <remarks>
/// The receipt neither generates nor hashes the archive: it fixes which receipts the archive holds.
/// The archive keeps its own signature (ADR 0007), and its integrity rests on the signatures and
/// chains of the entries inside it.
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

        // A training archive receipt prints the live totals and archives nothing (v1.2 TrainingRequest).
        if (!request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            var stateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
            stateData.FR ??= new MiddlewareStateDataFR();
            stateData.FR.Archive = await BoundariesAsync(request).ConfigureAwait(false);
            request.ReceiptResponse.ftStateData = stateData;
        }

        return await _pipeline.SignAsync(request).ConfigureAwait(false);
    }

    private async Task<FRArchiveBoundaries> BoundariesAsync(ProcessCommandRequest request)
    {
        var (previousArchiveQueueItemId, day, month, year) = await _chains.ReadAsync(queueFR =>
        {
            var totals = new FRPeriodTotalsAccumulator(queueFR);
            return (queueFR.ALastQueueItemId, totals.Snapshot(FRTotalsPeriod.Day).Current, totals.Snapshot(FRTotalsPeriod.Month).Current, totals.Snapshot(FRTotalsPeriod.Year).Current);
        }).ConfigureAwait(false);

        var queueItems = await _queueItems;
        var response = request.ReceiptResponse;

        // The last receipt the previous archives contained: archive receipts sent in a row contain
        // nothing, so walk back to the last one that did (v1.2 do/while).
        var lastArchived = await LastArchivedQueueItemIdAsync(queueItems, previousArchiveQueueItemId).ConfigureAwait(false);

        Guid? firstId = null;
        DateTime? firstMoment = null;
        ftQueueItem? lastAllowed = null;
        if (lastArchived is { } lastArchivedId)
        {
            // As v1.2, the next archive starts on the queue item the previous one ended on.
            var firstItem = await queueItems.GetAsync(lastArchivedId).ConfigureAwait(false);
            firstId = firstItem.ftQueueItemId;
            firstMoment = MomentOf(firstItem, response);
            if (firstItem.ftQueueItemId == response.ftQueueItemID)
            {
                lastAllowed = firstItem;
            }
            else if (firstMoment is { } from)
            {
                lastAllowed = await LastBeforeAsync(queueItems, firstItem.TimeStamp + 1, from.AddYears(1).Date).ConfigureAwait(false);
            }
        }
        else if (request.queue.StartMoment is { } start)
        {
            lastAllowed = await LastBeforeAsync(queueItems, 0, start.AddYears(1).Date).ConfigureAwait(false);
            var firstItem = await queueItems.GetEntriesOnOrAfterTimeStampAsync(0, 1).FirstOrDefaultAsync().ConfigureAwait(false);
            firstId = firstItem?.ftQueueItemId;
            firstMoment = firstItem is null ? null : MomentOf(firstItem, response);
        }

        // Null when the queue was unused for a year, or the archive receipt repeats the previous one:
        // the archive then ends where it starts (v1.2).
        Guid? lastId = firstId;
        DateTime? lastMoment = firstMoment;
        if (lastAllowed is not null)
        {
            lastId = lastAllowed.ftQueueItemId;
            lastMoment = MomentOf(lastAllowed, response);
        }

        return new FRArchiveBoundaries
        {
            PreviousArchiveQueueItemId = previousArchiveQueueItemId,
            FirstContainedReceiptQueueItemId = firstId,
            FirstContainedReceiptMoment = firstMoment,
            LastContainedReceiptQueueItemId = lastId,
            LastContainedReceiptMoment = lastMoment,
            LastReceiptJournalId = (await (await _receiptJournals).GetWithLastTimestampAsync().ConfigureAwait(false))?.ftReceiptJournalId,
            LastActionJournalId = (await (await _actionJournals).GetWithLastTimestampAsync().ConfigureAwait(false))?.ftActionJournalId,
            LastJournalFRId = null,
            Day = day,
            Month = month,
            Year = year,
        };
    }

    private static async Task<Guid?> LastArchivedQueueItemIdAsync(IMiddlewareQueueItemRepository queueItems, Guid? archiveQueueItemId)
    {
        var visited = new HashSet<Guid>();
        while (archiveQueueItemId is { } id && visited.Add(id))
        {
            var archive = await queueItems.GetAsync(id).ConfigureAwait(false);
            var boundaries = archive?.response is { Length: > 0 } stored ? BoundariesOf(stored) : null;
            if (boundaries is null)
            {
                return null;
            }

            if (boundaries.LastContainedReceiptQueueItemId is { } last)
            {
                return last;
            }

            archiveQueueItemId = boundaries.PreviousArchiveQueueItemId;
        }

        return null;
    }

    /// <summary>A v2 archive receipt keeps its boundaries in its state data; one v1.2 signed keeps them in its receipt signature's payload.</summary>
    private static FRArchiveBoundaries? BoundariesOf(string storedResponse)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(storedResponse);
        if (response is null)
        {
            return null;
        }

        if (FRStateData.Archive(response) is { } boundaries)
        {
            return boundaries;
        }

        var v1Signature = response.ftSignatures?.FirstOrDefault(x => (long) x.ftSignatureType == V1ReceiptSignatureType)?.Data;
        var parts = v1Signature?.Split('.');
        if (parts is not { Length: 3 })
        {
            return null;
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return JsonSerializer.Deserialize<FRArchiveBoundaries>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static async Task<ftQueueItem?> LastBeforeAsync(IMiddlewareQueueItemRepository queueItems, long fromTimeStamp, DateTime before)
    {
        ftQueueItem? last = null;
        await foreach (var item in queueItems.GetEntriesOnOrAfterTimeStampAsync(fromTimeStamp).ConfigureAwait(false))
        {
            if (item.ftQueueMoment < before && (last is null || item.TimeStamp > last.TimeStamp))
            {
                last = item;
            }
        }

        return last;
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
