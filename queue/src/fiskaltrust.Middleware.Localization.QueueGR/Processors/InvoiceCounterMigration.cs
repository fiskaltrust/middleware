using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

/// <summary>
/// One-time initialization of the dedicated GR invoice counter. Runs at queue start
/// (QueueGRBootstrapper awaits it before any receipt is processed): if the counter
/// cannot be initialized, receipt processing fails loudly instead of silently falling
/// back to the legacy ftReceiptNumerator-derived numbering.
/// </summary>
public static class InvoiceCounterMigration
{
    public static async Task EnsureMigratedAsync(
        IConfigurationRepository configurationRepository,
        IMiddlewareQueueItemRepository queueItemRepository,
        Guid queueId,
        ILogger logger)
    {
        var queueGR = await configurationRepository.GetQueueGRAsync(queueId);
        if (!string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            return;
        }

        // The seed comes from the queue-item history: the pre-counter MyDataSCU
        // appended "{series}-{aa}" to ftReceiptIdentification on every successful
        // submission (and only then), so successful responses carrying the queue's own
        // series are exactly the invoices filed under it. ftReceiptNumerator cannot
        // provide the seed: it advances for every successful receipt, including zero
        // receipts and closings that never reach AADE.
        var queue = await configurationRepository.GetQueueAsync(queueId);
        queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
        queueGR.InvoiceNumerator = await GetLastSubmittedAaAsync(queueItemRepository, queueGR.InvoiceSeries, queue.ftQueuedRow);
        await configurationRepository.InsertOrUpdateQueueGRAsync(queueGR);

        logger.LogInformation(
            "QueueGR invoice counter migration for queue {QueueId}: seeded InvoiceSeries '{InvoiceSeries}' at InvoiceNumerator {InvoiceNumerator} (ftReceiptNumerator: {ReceiptNumerator}, ftQueuedRow: {QueuedRow}).",
            queueId, queueGR.InvoiceSeries, queueGR.InvoiceNumerator, queue.ftReceiptNumerator, queue.ftQueuedRow);
        if (queueGR.InvoiceNumerator > queue.ftReceiptNumerator)
        {
            // Automatic numbering always used aa == ftReceiptNumerator, so a seed above
            // it can only come from a historical caller-numbered submission (an aa-only
            // mydataoverride into the queue's own series). The continuation is safe —
            // strictly above everything filed — but the jump deserves to be visible.
            logger.LogWarning(
                "QueueGR invoice counter migration for queue {QueueId}: the seed {InvoiceNumerator} exceeds ftReceiptNumerator {ReceiptNumerator} — a historical submission carried a caller-chosen aa in the queue's own series. The sequence continues above it.",
                queueId, queueGR.InvoiceNumerator, queue.ftReceiptNumerator);
        }
    }

    /// <summary>
    /// Rows per GetByQueueRowRangeAsync query. Small enough to bound the queue items
    /// held in memory (requests and responses included), large enough that the common
    /// case — the newest submission lies within the last few hundred rows — is a single
    /// roundtrip to storage.
    /// </summary>
    private const int ChunkSize = 500;

    private static async Task<long> GetLastSubmittedAaAsync(
        IMiddlewareQueueItemRepository queueItemRepository,
        string invoiceSeries,
        long lastQueueRow)
    {
        // Walk newest → oldest and stop at the FIRST successful response carrying a
        // "{series}-{aa}" segment in the queue's own series. The history is read in
        // row-range chunks — one storage query per ChunkSize rows instead of one per
        // row (per-row reads caused a read storm on Azure Table Storage, where every
        // ftQueueRow lookup is an unpartitioned filter query). The range query gives no
        // ordering guarantee (partition-key schemes differ between eras), so each chunk
        // is ordered client-side before the newest-first scan.
        //
        // Accepted risk of seeding from the newest instead of the history maximum: the
        // old mydataoverride path could file a caller-chosen aa into the queue's own
        // series (an aa-only override kept series = CashBoxIdentification). If such
        // out-of-order values exist and the newest own-series aa is not the largest,
        // this seed is too low and the next reservation is rejected by AADE as a
        // duplicate (233) — a loud, per-receipt failure that needs a manual counter
        // correction, since nothing self-heals 233 yet. Overrides into the own series
        // are rejected since c43de72a, so only pre-existing history can trigger this.
        //
        // Success responses without a segment (zero receipts, closings, lifecycle
        // receipts) and submissions in a foreign series (handwritten) are skipped.
        // Error responses are skipped too: failed attempts must never count as
        // submitted.
        for (var chunkEnd = lastQueueRow; chunkEnd >= 1; chunkEnd -= ChunkSize)
        {
            var chunkStart = Math.Max(1, chunkEnd - ChunkSize + 1);
            var chunk = new List<ftQueueItem>();
            await foreach (var queueItem in queueItemRepository.GetByQueueRowRangeAsync(chunkStart, chunkEnd))
            {
                chunk.Add(queueItem);
            }
            foreach (var queueItem in chunk.OrderByDescending(x => x.ftQueueRow))
            {
                if (TryGetSubmittedAa(queueItem, invoiceSeries, out var aa))
                {
                    return aa;
                }
            }
        }
        return 0;
    }

    private static bool TryGetSubmittedAa(ftQueueItem queueItem, string invoiceSeries, out long aa)
    {
        aa = 0;
        if (string.IsNullOrEmpty(queueItem?.response))
        {
            return false;
        }
        ReceiptResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<ReceiptResponse>(queueItem!.response);
        }
        catch (JsonException)
        {
            return false;
        }
        if (response == null || !response.ftState.IsState(State.Success))
        {
            return false;
        }
        return CountrySegment.TryParse(response.ftReceiptIdentification, out var series, out aa)
            && string.Equals(series, invoiceSeries, StringComparison.Ordinal);
    }
}

/// <summary>
/// Runs the migration eagerly at queue construction and is awaited before every
/// receipt. A faulted attempt is retried on the next receipt instead of being cached
/// forever, so a transient storage error at startup only fails the receipts processed
/// during the outage — not every receipt until the process restarts.
/// </summary>
public sealed class InvoiceCounterMigrationGate
{
    private readonly Func<Task> _migrate;
    private readonly object _sync = new();
    private Task _attempt;

    public InvoiceCounterMigrationGate(Func<Task> migrate)
    {
        _migrate = migrate;
        _attempt = Task.Run(migrate);
    }

    public Task EnsureMigratedAsync()
    {
        lock (_sync)
        {
            if (_attempt.IsFaulted || _attempt.IsCanceled)
            {
                _attempt = Task.Run(_migrate);
            }
            return _attempt;
        }
    }
}
