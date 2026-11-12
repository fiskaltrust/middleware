using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
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
        // Defensive, as v1.3 ArchiveCommand ([04 F12]): the numerator only moves after the initial-operation receipt, so on an
        // active queue this never fires; v1.2 ArchiveRequest has no such guard.
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
        List<FRChainIntegrity.Defect> defects = [];
        if (!request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            var stateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
            stateData.FR ??= new MiddlewareStateDataFR();
            FRArchiveBoundaries boundaries;
            try
            {
                boundaries = await BoundariesAsync(request, previousArchiveQueueItemId, day, month, year).ConfigureAwait(false);
            }
            catch (ArchiveRangeException ex)
            {
                // Signing a range that cannot be fixed would leave records in no signed range (contract §1.4,
                // "every record lies in exactly one range"): the receipt is refused, nothing is signed.
                request.ReceiptResponse.SetReceiptResponseError(ex.Message);
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }
            stateData.FR.Archive = boundaries;
            request.ReceiptResponse.ftStateData = stateData;
            defects = await IntegrityDefectsAsync(previousArchiveQueueItemId, request.ReceiptResponse.ftQueueItemID).ConfigureAwait(false);
        }

        var response = await _pipeline.SignAsync(request).ConfigureAwait(false);
        if (defects.Count == 0 || !FRSigningPipeline.Succeeded(response))
        {
            return response;
        }

        // ADR 0009, #208: a defect found while handling the request is reported, never blocking: the archive
        // receipt carries one JET item per code in its own response, and is signed all the same.
        foreach (var (code, label, information) in FRChainIntegrity.PerCode(defects))
        {
            response.receiptResponse.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(code, label, information));
        }

        return response;
    }

    /// <summary>
    /// Checks the chains of the range this archive receipt closes (<see cref="FRChainIntegrity"/>): the signed
    /// entries after the previous archive receipt, and each chain's last entry against the chain state.
    /// </summary>
    private async Task<List<FRChainIntegrity.Defect>> IntegrityDefectsAsync(Guid? previousArchiveQueueItemId, Guid currentQueueItemId)
    {
        var queueItems = await _queueItems;
        var from = 0L;
        if (previousArchiveQueueItemId is { } previousId && await queueItems.GetAsync(previousId).ConfigureAwait(false) is { } previous)
        {
            from = previous.TimeStamp + 1;
        }

        var entries = new List<(ArchiveEntry?, ArchiveEntryException?)>();
        await foreach (var item in queueItems.GetEntriesOnOrAfterTimeStampAsync(from).ConfigureAwait(false))
        {
            if (item.ftQueueItemId == currentQueueItemId)
            {
                break;
            }

            try
            {
                entries.Add((ArchiveEntry.Read(item.ftQueueItemId, item.ftQueueId, item.ftQueueRow, item.request, item.response), null));
            }
            catch (ArchiveEntryException ex)
            {
                entries.Add((null, ex));
            }
        }

        return await _chains.ReadAsync(queueFR => FRChainIntegrity.Check(entries, queueFR)).ConfigureAwait(false);
    }

    /// <summary>The boundaries of the archive this receipt closes.</summary>
    /// <exception cref="ArchiveRangeException">The previous archive receipt, or an entry of the range, cannot be found or read.</exception>
    private async Task<FRArchiveBoundaries> BoundariesAsync(ProcessCommandRequest request, Guid? previousArchiveQueueItemId, FRTotals day, FRTotals month, FRTotals year)
    {
        var queueItems = await _queueItems;
        var response = request.ReceiptResponse;

        ftQueueItem? previous = null;
        if (previousArchiveQueueItemId is { } previousId)
        {
            previous = await queueItems.GetAsync(previousId).ConfigureAwait(false)
                ?? throw new ArchiveRangeException($"The previous archive receipt (queue item {previousId}) cannot be found in storage, so the range this archive receipt closes cannot be fixed. No archive receipt is signed until the storage holds it again.");
        }
        var (firstId, firstMoment) = await FirstContainedAsync(queueItems, previous, response).ConfigureAwait(false);

        return new FRArchiveBoundaries
        {
            PreviousArchiveQueueItemId = previousArchiveQueueItemId,
            FirstContainedReceiptQueueItemId = firstId,
            FirstContainedReceiptMoment = firstMoment,
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
    /// The first record of the archive: the first signed entry after the previous archive receipt, or the queue's
    /// first signed entry, read as the archive export reads entries (<see cref="ArchiveEntry.Read"/>): items without
    /// a response, error responses and unsigned responses are skipped, so the record always has a moment. When no
    /// signed entry lies between, the archive receipt is its own first record. After a half migration, a previous
    /// archive receipt that v1.2 signed belongs to no v1.2 archive (v1.2 put it in the next one), so the v2 range
    /// starts on it.
    /// </summary>
    private static async Task<(Guid Id, DateTime Moment)> FirstContainedAsync(IMiddlewareQueueItemRepository queueItems, ftQueueItem? previous, ReceiptResponse current)
    {
        var from = 0L;
        if (previous is not null)
        {
            if (SignedByV12(previous))
            {
                return (previous.ftQueueItemId, ReceiptMomentOrRefuse(previous, $"The previous archive receipt (queue item {previous.ftQueueItemId})"));
            }
            from = previous.TimeStamp + 1;
        }

        await foreach (var item in queueItems.GetEntriesOnOrAfterTimeStampAsync(from).ConfigureAwait(false))
        {
            if (item.ftQueueItemId == current.ftQueueItemID)
            {
                break;
            }
            if (SignedMoment(item) is { } moment)
            {
                return (item.ftQueueItemId, moment);
            }
        }
        return (current.ftQueueItemID, current.ftReceiptMoment);
    }

    /// <summary>The receipt moment of a signed entry (v2, or v1.2 after a half migration); null for an item that signed nothing.</summary>
    private static DateTime? SignedMoment(ftQueueItem item)
    {
        ArchiveEntry? entry;
        try
        {
            entry = ArchiveEntry.Read(item.ftQueueItemId, item.ftQueueId, item.ftQueueRow, item.request, item.response);
        }
        catch (ArchiveEntryException ex)
        {
            throw new ArchiveRangeException($"An entry of the range this archive receipt closes cannot be read, so the range cannot be fixed: {ex.Message}");
        }
        if (entry is not null)
        {
            return entry.ReceiptMoment;
        }
        return HasV1ReceiptSignature(item) ? ReceiptMomentOrRefuse(item, $"The v1.2 entry {item.ftQueueItemId} (queue row {item.ftQueueRow})") : null;
    }

    private static DateTime ReceiptMomentOrRefuse(ftQueueItem item, string what)
        => ArchiveEntry.ReceiptMomentOf(item.response)
            ?? throw new ArchiveRangeException($"{what} cannot be read: its stored response holds no readable ftReceiptMoment, so the range this archive receipt closes cannot be fixed.");

    /// <summary>A v2 archive receipt keeps its boundaries in its state data; one v1.2 signed has none, only its v1 receipt signature.</summary>
    /// <exception cref="ArchiveRangeException">The stored response cannot be read.</exception>
    private static bool SignedByV12(ftQueueItem archive)
    {
        if (string.IsNullOrEmpty(archive.response))
        {
            return false;
        }

        ReceiptResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<ReceiptResponse>(archive.response);
        }
        catch (JsonException ex)
        {
            throw new ArchiveRangeException($"The previous archive receipt (queue item {archive.ftQueueItemId}) cannot be read: its stored response is no valid receipt response ({ex.Message}). No archive receipt is signed until it can be read.");
        }
        return response is not null
            && FRStateData.Archive(response) is null
            && response.ftSignatures?.Any(x => (long) x.ftSignatureType == V1ReceiptSignatureType) == true;
    }

    /// <summary>Whether a stored response carries a v1 receipt signature (0x4652_0000_0000_0001, v1 receipt signature); false when it cannot be parsed.</summary>
    private static bool HasV1ReceiptSignature(ftQueueItem item)
    {
        if (string.IsNullOrEmpty(item.response))
        {
            return false;
        }
        try
        {
            using var document = JsonDocument.Parse(item.response);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("ftSignatures", out var signatures)
                && signatures.ValueKind == JsonValueKind.Array
                && signatures.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.Object
                    && x.TryGetProperty("ftSignatureType", out var type)
                    && type.ValueKind == JsonValueKind.Number && type.TryGetInt64(out var value) && value == V1ReceiptSignatureType);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>The range of an archive receipt cannot be fixed; the receipt is refused.</summary>
    private sealed class ArchiveRangeException : Exception
    {
        public ArchiveRangeException(string message) : base(message) { }
    }
}
