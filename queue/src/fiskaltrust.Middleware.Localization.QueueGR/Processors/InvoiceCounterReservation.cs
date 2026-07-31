using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

internal static class InvoiceCounterReservation
{
    public static async Task<ProcessCommandResponse> InvokeWithCounterAsync(
        ProcessCommandRequest request,
        AsyncLazy<IConfigurationRepository> configurationRepository,
        AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository,
        Func<Task<ProcessResponse>> sscdCall)
    {
        var configRepo = await configurationRepository;
        var queueGR = await configRepo.GetQueueGRAsync(request.queue.ftQueueId);

        // One-time migration for queues activated before this code shipped. The reliable
        // upgrade discriminator is an empty InvoiceSeries: LifecycleCommandProcessorGR
        // now seeds it from CashBoxIdentification at activation, so every new queue has
        // it populated before its first submission. A queue that reaches this point with
        // InvoiceSeries still unset went through activation under the old code and never
        // got the seed — i.e. it is a pre-upgrade queue.
        //
        // The seed must guarantee that the first post-upgrade aa is exactly
        // last-submitted-aa + 1. ftReceiptNumerator cannot provide that: it advances for
        // every successful receipt, including zero receipts, daily/monthly/yearly
        // closings and other NoOps that never reach AADE, so seeding from it would skip
        // one aa per intervening NoOp. Instead we recover the last aa that actually went
        // to AADE from the queue-item history: the pre-upgrade MyDataSCU appended
        // "{series}-{aa}" to ftReceiptIdentification on every successful submission (and
        // only then), so the newest successful response carrying our own series is
        // exactly the last submitted invoice. NoOp receipts never carry that segment and
        // therefore can never influence the counter.
        //
        // The seed is persisted immediately so it survives a failed first submission and
        // the history scan runs at most once per queue.
        //
        // Deliberate residual risk: an attempt that AADE accepted but that was stored as
        // failed on our side (timeout/crash between AADE's 200 OK and persisting the
        // response) has no success response in the history, so its aa is re-reserved and
        // AADE will reject the resubmission with error 233 (duplicate uid) instead of us
        // skipping a number. Gap-free continuity is the requirement here; recovering
        // from 233 is the planned self-heal follow-up.
        if (string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
            queueGR.InvoiceNumerator = await GetLastSubmittedAaAsync(
                await queueItemRepository,
                queueGR.InvoiceSeries,
                request.ReceiptResponse.ftQueueRow);
            await configRepo.InsertOrUpdateQueueGRAsync(queueGR);
        }

        var reservedSeries = queueGR.InvoiceSeries;
        var reservedAa = queueGR.InvoiceNumerator + 1;

        // Pre-append the country segment to ftReceiptIdentification, following the same
        // convention every other country queue uses (ES/FR/AT/PT all append after "#").
        // AADEFactory reads (series, aa) from this segment; MyDataSCU rewrites it after
        // AADE confirms what was actually submitted, so an override path (handwritten
        // or mydataoverride) produces a segment different from our reservation and the
        // commit check below correctly skips advancing the counter.
        var originalReceiptIdentification = request.ReceiptResponse.ftReceiptIdentification;
        request.ReceiptResponse.ftReceiptIdentification += $"{reservedSeries}-{reservedAa}";

        ProcessResponse response;
        try
        {
            response = await sscdCall();
        }
        catch
        {
            // The SCU call failed outright, so the reservation was never confirmed.
            // Failed receipts must persist exactly the identification they had before
            // this feature existed ("ft{N:X}#") — restore it before the exception
            // reaches SignProcessor, which persists the response as failed.
            request.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
            throw;
        }

        if (WasReservedCounterUsed(response.ReceiptResponse, reservedSeries, reservedAa))
        {
            queueGR.InvoiceNumerator = reservedAa;
            queueGR.LastInvoiceMoment = request.ReceiptRequest.cbReceiptMoment;
            queueGR.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueGR.LastInvoiceMark = TryExtractMark(response.ReceiptResponse);
            await configRepo.InsertOrUpdateQueueGRAsync(queueGR);
        }
        else if (!response.ReceiptResponse.ftState.IsState(State.Success))
        {
            // MyDataSCU rewrites the country segment only on success, so an
            // unsuccessful response still carries our unconfirmed reservation. Failed
            // receipts must look exactly like they did before this feature — restore
            // the pre-reservation identification. (A successful response that didn't
            // use the reservation keeps its segment: it holds the handwritten or
            // override values that actually went to AADE, same as the old behaviour.)
            response.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
        }

        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    private static async Task<long> GetLastSubmittedAaAsync(
        IMiddlewareQueueItemRepository queueItemRepository,
        string invoiceSeries,
        long currentQueueRow)
    {
        // Walk the queue-item history backwards from the receipt currently being
        // processed until we find the newest response that was (a) successful and
        // (b) carries a "{series}-{aa}" segment in our own series — i.e. the last
        // invoice that was actually submitted to AADE. Success responses without a
        // segment (zero receipts, closings, lifecycle receipts) and submissions in a
        // foreign series (handwritten / mydataoverride) are skipped. Error responses
        // are skipped too: attempts made by this code carry the reserved segment even
        // when they fail, and treating them as submitted would drift the seed upwards
        // on every retry.
        //
        // The scan is unbounded on purpose. Capping it and falling back to a guess
        // could re-issue an aa that AADE already has on file. The cost self-limits:
        // queues that submit regularly find the segment within the last few rows, and
        // queues that never submitted are small because they only ever collected NoOps.
        // The scan runs once per queue — the seed is persisted right after.
        for (var row = currentQueueRow - 1; row >= 1; row--)
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
            if (TryParseCountrySegment(response.ftReceiptIdentification, out var series, out var aa)
                && string.Equals(series, invoiceSeries, StringComparison.Ordinal))
            {
                return aa;
            }
        }
        return 0;
    }

    private static bool WasReservedCounterUsed(ReceiptResponse response, string series, long aa)
    {
        // Commit only if AADE confirmed the submission *and* it used our reservation.
        // MyDataSCU rewrites the country segment after "#" with the (series, aa) that
        // actually went to AADE on a successful submission. If that segment still equals
        // our reservation, the auto-counter was honoured; if not, a handwritten or
        // mydataoverride path replaced it and we must not advance.
        if (!response.ftState.IsState(State.Success))
        {
            return false;
        }
        return TryParseCountrySegment(response.ftReceiptIdentification, out var actualSeries, out var actualAa)
            && string.Equals(actualSeries, series, StringComparison.Ordinal)
            && actualAa == aa;
    }

    private static bool TryParseCountrySegment(string? receiptIdentification, out string series, out long aa)
    {
        // The country segment is everything after the first "#":  "ft{N:X}#{series}-{aa}".
        // The series itself may contain dashes, so the aa is split off at the last one.
        series = string.Empty;
        aa = 0;
        if (string.IsNullOrEmpty(receiptIdentification))
        {
            return false;
        }
        var hashIdx = receiptIdentification.IndexOf('#');
        if (hashIdx < 0 || hashIdx >= receiptIdentification.Length - 1)
        {
            return false;
        }
        var segment = receiptIdentification.Substring(hashIdx + 1);
        var dashIdx = segment.LastIndexOf('-');
        if (dashIdx <= 0 || dashIdx >= segment.Length - 1)
        {
            return false;
        }
        if (!long.TryParse(segment.Substring(dashIdx + 1), NumberStyles.None, CultureInfo.InvariantCulture, out aa))
        {
            return false;
        }
        series = segment.Substring(0, dashIdx);
        return true;
    }

    private static long? TryExtractMark(ReceiptResponse response)
    {
        var markSignature = response.ftSignatures?
            .FirstOrDefault(s => string.Equals(s.Caption, "invoiceMark", StringComparison.Ordinal));
        return markSignature != null && long.TryParse(markSignature.Data, out var mark)
            ? mark
            : (long?) null;
    }
}
