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

    private static async Task<long> GetLastSubmittedAaAsync(
        IMiddlewareQueueItemRepository queueItemRepository,
        string invoiceSeries,
        long lastQueueRow)
    {
        // Walk newest → oldest and stop at the FIRST successful response carrying a
        // "{series}-{aa}" segment in the queue's own series. On Azure Table Storage
        // every GetByQueueRowAsync is an unpartitioned filter query, so this early exit
        // is what keeps queue start cheap: on any queue that ever submitted, only the
        // NoOps and failed attempts since the last real submission are read — walking
        // the complete history here caused a read storm on large queues.
        //
        // Accepted risk of seeding from the newest instead of the history maximum: the
        // old mydataoverride path could file a caller-chosen aa into the queue's own
        // series (an aa-only override kept series = CashBoxIdentification), so the
        // newest own-series aa is not necessarily the largest and a too-low seed can
        // re-issue numbers used further back. AADE's duplicate detection is UID-based
        // (a hash over issuer, issue date, branch, invoice type, series and aa —
        // myDATA API v1.0.10 §7.2, error 233 "UID has already been sent"): a same-day
        // re-issue is rejected as 233 and InvoiceCounterReservation advances the
        // counter past it ("number consumed"), while a re-issue on a later date has a
        // different UID and files as a new invoice — the sequence continues, at the
        // price of a re-used aa in the series history. Overrides into the own series
        // are rejected since c43de72a, so only pre-existing history can trigger this.
        //
        // Success responses without a segment (zero receipts, closings, lifecycle
        // receipts) and submissions in a foreign series (handwritten) are skipped.
        // Error responses are skipped too: failed attempts must never count as
        // submitted.
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
                && string.Equals(series, invoiceSeries, StringComparison.Ordinal))
            {
                return aa;
            }
        }
        return 0;
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
