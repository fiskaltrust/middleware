using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

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
        Guid queueId)
    {
        var queueGR = await configurationRepository.GetQueueGRAsync(queueId);
        if (!string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            return;
        }

        // The seed must guarantee that the first counter-based aa is exactly
        // last-submitted-aa + 1. ftReceiptNumerator cannot provide that: it advances for
        // every successful receipt, including zero receipts and daily/monthly/yearly
        // closings that never reach AADE. Instead we recover the last aa that actually
        // went to AADE from the queue-item history: the pre-counter MyDataSCU appended
        // "{series}-{aa}" to ftReceiptIdentification on every successful submission (and
        // only then), so the newest successful response carrying the queue's own series
        // is exactly the last submitted invoice. NoOp receipts never carry that segment
        // and can therefore never influence the counter.
        //
        // Deliberate residual risk: an attempt that AADE accepted but that was stored as
        // failed on our side (timeout/crash between AADE's 200 OK and persisting the
        // response) has no success response in the history, so its aa is re-reserved and
        // AADE will reject the resubmission with error 233 (duplicate uid) instead of us
        // skipping a number. Gap-free continuity is the requirement here; recovering
        // from 233 is the planned self-heal follow-up.
        var queue = await configurationRepository.GetQueueAsync(queueId);
        queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
        queueGR.InvoiceNumerator = await GetLastSubmittedAaAsync(queueItemRepository, queueGR.InvoiceSeries, queue.ftQueuedRow);
        await configurationRepository.InsertOrUpdateQueueGRAsync(queueGR);
    }

    private static async Task<long> GetLastSubmittedAaAsync(
        IMiddlewareQueueItemRepository queueItemRepository,
        string invoiceSeries,
        long lastQueueRow)
    {
        // Walk the queue-item history backwards until we find the newest response that
        // was (a) successful and (b) carries a "{series}-{aa}" segment in the queue's
        // own series — i.e. the last invoice that was actually submitted to AADE.
        // Success responses without a segment (zero receipts, closings, lifecycle
        // receipts) and submissions in a foreign series (handwritten / mydataoverride)
        // are skipped. Error responses are skipped too: failed attempts must never count
        // as submitted.
        //
        // The scan is unbounded on purpose. Capping it and falling back to a guess
        // could re-issue an aa that AADE already has on file. The cost self-limits:
        // queues that submit regularly find the segment within the last few rows, and
        // queues that never submitted are small because they only ever collected NoOps.
        // The scan runs once per queue — the seed is persisted right after.
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
