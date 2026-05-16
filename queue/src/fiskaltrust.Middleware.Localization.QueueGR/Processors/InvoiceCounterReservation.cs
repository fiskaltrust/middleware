using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

internal static class InvoiceCounterReservation
{
    public static async Task<ProcessCommandResponse> InvokeWithCounterAsync(
        ProcessCommandRequest request,
        AsyncLazy<IConfigurationRepository> configurationRepository,
        Func<Task<ProcessResponse>> sscdCall)
    {
        var configRepo = await configurationRepository;
        var queueGR = await configRepo.GetQueueGRAsync(request.queue.ftQueueId);

        // One-time migration for queues activated before this code shipped. Pre-upgrade,
        // the aa transmitted to AADE was derived from the generic ftReceiptNumerator.
        // We seed InvoiceNumerator from that value so the first post-upgrade aa lands
        // strictly above any aa AADE already has on file for this queue, avoiding the
        // deterministic-uid collision. The gap produced (we overshoot by the count of
        // NoOp receipts that previously advanced ftReceiptNumerator without submitting)
        // is acceptable — AADE permits non-contiguous aa, it just refuses duplicates.
        if (string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            queueGR.InvoiceSeries = queueGR.CashBoxIdentification;
        }
        if (queueGR.InvoiceNumerator == 0 && request.queue.ftReceiptNumerator > 0)
        {
            queueGR.InvoiceNumerator = request.queue.ftReceiptNumerator;
        }

        var reservedSeries = queueGR.InvoiceSeries;
        var reservedAa = queueGR.InvoiceNumerator + 1;

        // Pre-append the country segment to ftReceiptIdentification, following the same
        // convention every other country queue uses (ES/FR/AT/PT all append after "#").
        // AADEFactory reads (series, aa) from this segment; MyDataSCU rewrites it after
        // AADE confirms what was actually submitted, so an override path (handwritten
        // or mydataoverride) produces a suffix different from our reservation and the
        // commit check below correctly skips advancing the counter.
        request.ReceiptResponse.ftReceiptIdentification += $"{reservedSeries}-{reservedAa}";

        var response = await sscdCall();

        if (WasReservedCounterUsed(response.ReceiptResponse, reservedSeries, reservedAa))
        {
            queueGR.InvoiceNumerator = reservedAa;
            queueGR.LastInvoiceMoment = request.ReceiptRequest.cbReceiptMoment;
            queueGR.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueGR.LastInvoiceMark = TryExtractMark(response.ReceiptResponse);
            await configRepo.InsertOrUpdateQueueGRAsync(queueGR);
        }

        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    private static bool WasReservedCounterUsed(ReceiptResponse response, string series, long aa)
    {
        // Commit only if AADE confirmed the submission *and* it used our reservation.
        // MyDataSCU rewrites the country segment after "#" with the (series, aa) that
        // actually went to AADE on a successful submission (including the 233 retry
        // branch). If the rewrite still matches our reservation, the auto-counter was
        // honoured; if not, a handwritten or mydataoverride path replaced it and we
        // must not advance.
        if (!response.ftState.IsState(State.Success))
        {
            return false;
        }
        var identification = response.ftReceiptIdentification;
        return !string.IsNullOrEmpty(identification)
            && identification.EndsWith($"{series}-{aa}", StringComparison.Ordinal);
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
