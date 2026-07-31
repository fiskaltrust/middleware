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

        // The seed must guarantee that the first counter-based aa is strictly above
        // every aa AADE already has on file in the queue's own series.
        // ftReceiptNumerator cannot provide that: it advances for every successful
        // receipt, including zero receipts and closings that never reach AADE. Instead
        // we recover the submitted aa values from the queue-item history: the
        // pre-counter MyDataSCU appended "{series}-{aa}" to ftReceiptIdentification on
        // every successful submission (and only then), so successful responses carrying
        // the queue's own series are exactly the invoices filed under it. NoOp receipts
        // never carry that segment and can therefore never influence the counter.
        var queue = await configurationRepository.GetQueueAsync(queueId);
        queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
        queueGR.InvoiceNumerator = await GetMaxSubmittedAaAsync(queueItemRepository, queueGR.InvoiceSeries, queue.ftQueuedRow);
        await configurationRepository.InsertOrUpdateQueueGRAsync(queueGR);

        logger.LogInformation(
            "QueueGR invoice counter migration for queue {QueueId}: seeded InvoiceSeries '{InvoiceSeries}' at InvoiceNumerator {InvoiceNumerator} (ftReceiptNumerator: {ReceiptNumerator}, rows scanned: {Rows}).",
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

    private static async Task<long> GetMaxSubmittedAaAsync(
        IMiddlewareQueueItemRepository queueItemRepository,
        string invoiceSeries,
        long lastQueueRow)
    {
        // Walk the complete queue-item history and take the MAXIMUM aa among successful
        // responses carrying a "{series}-{aa}" segment in the queue's own series — not
        // the newest one. The old mydataoverride path could file a caller-chosen aa
        // into the queue's own series (an aa-only override kept series =
        // CashBoxIdentification): if such a submission is newer than the last automatic
        // one, a newest-based seed would restart below already-filed values and every
        // following reservation would collide at AADE (error 233, which nothing
        // self-heals yet). The maximum is collision-free by construction.
        //
        // Success responses without a segment (zero receipts, closings, lifecycle
        // receipts) and submissions in a foreign series (handwritten) are skipped.
        // Error responses are skipped too: failed attempts must never count as
        // submitted.
        //
        // The walk is one GetByQueueRowAsync per row (a filtered query on Azure Table
        // Storage, which finds rows regardless of their partition-key era) and runs
        // once per queue: the seed is persisted right after, and the queue-start gate
        // keeps receipts waiting until it completed.
        var maxAa = 0L;
        for (var row = lastQueueRow; row >= 1; row--)
        {
            var queueItem = await queueItemRepository.GetByQueueRowAsync(row);
            if (string.IsNullOrEmpty(queueItem?.response))
            {
                continue;
            }
            ReceiptResponse? response;
            try
            {
                response = JsonSerializer.Deserialize<ReceiptResponse>(queueItem!.response);
            }
            catch (JsonException)
            {
                continue;
            }
            if (response == null || !response.ftState.IsState(State.Success))
            {
                continue;
            }
            if (CountrySegment.TryParse(response.ftReceiptIdentification, out var series, out var aa)
                && string.Equals(series, invoiceSeries, StringComparison.Ordinal)
                && aa > maxAa)
            {
                maxAa = aa;
            }
        }
        return maxAa;
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
