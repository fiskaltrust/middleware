using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Models;
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
        // Handwritten documents are caller-numbered: the merchant already stamped
        // (series, aa) on the paper original, so the queue takes them inbound verbatim.
        // No reservation is made, the counter never advances and no storage is touched.
        // Full validation of the handwritten payload stays in the SCU.
        if (TryGetHandwrittenNumbering(request.ReceiptRequest, out var handwrittenSeries, out var handwrittenAa))
        {
            var handwrittenResponse = await SubmitWithSegmentAsync(request, handwrittenSeries, handwrittenAa, sscdCall);
            return new ProcessCommandResponse(handwrittenResponse.ReceiptResponse, []);
        }

        var configRepo = await configurationRepository;
        var queueGR = await configRepo.GetQueueGRAsync(request.queue.ftQueueId);

        // The invoice counter is initialized once at queue start (InvoiceCounterMigration
        // is awaited before any receipt is processed) and at activation for fresh queues.
        // If the series is still unset here something is genuinely broken — refuse to
        // number rather than falling back to any legacy scheme.
        if (string.IsNullOrEmpty(queueGR.InvoiceSeries))
        {
            throw new InvalidOperationException(
                $"The GR invoice counter of queue {request.queue.ftQueueId} is not initialized — the startup migration has not run. Refusing to submit with legacy numbering.");
        }

        var reservedSeries = queueGR.InvoiceSeries;
        var reservedAa = queueGR.InvoiceNumerator + 1;

        var response = await SubmitWithSegmentAsync(request, reservedSeries, reservedAa, sscdCall);

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

    private static bool TryGetHandwrittenNumbering(ReceiptRequest request, out string series, out long aa)
    {
        series = string.Empty;
        aa = 0;
        if (!request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            return false;
        }
        if (!request.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var payload)
            || string.IsNullOrEmpty(payload?.GR?.Series)
            || payload!.GR!.AA is not > 0)
        {
            // Incomplete handwritten payload: fall through to the reservation path. The
            // SCU rejects the request with its precise validation error, the reservation
            // is never confirmed, and the counter does not advance.
            return false;
        }
        series = payload.GR.Series!;
        aa = payload.GR.AA!.Value;
        return true;
    }

    private static async Task<ProcessResponse> SubmitWithSegmentAsync(
        ProcessCommandRequest request,
        string series,
        long aa,
        Func<Task<ProcessResponse>> sscdCall)
    {
        // Pre-append the country segment to ftReceiptIdentification, following the same
        // convention every other country queue uses (ES/FR/AT/PT all append after "#").
        // AADEFactory reads (series, aa) from this segment; MyDataSCU rewrites it after
        // AADE confirms what was actually submitted.
        var originalReceiptIdentification = request.ReceiptResponse.ftReceiptIdentification;
        request.ReceiptResponse.ftReceiptIdentification += $"{series}-{aa}";

        ProcessResponse response;
        try
        {
            response = await sscdCall();
        }
        catch
        {
            // The SCU call failed outright, so the segment was never confirmed. Failed
            // receipts must persist exactly the identification they had before this
            // feature existed ("ft{N:X}#") — restore it before the exception reaches
            // SignProcessor, which persists the response as failed.
            request.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
            throw;
        }

        if (!response.ReceiptResponse.ftState.IsState(State.Success))
        {
            // MyDataSCU rewrites the country segment only on success, so an unsuccessful
            // response still carries the unconfirmed segment. Failed receipts must look
            // exactly like they did before this feature — restore the pre-segment
            // identification.
            response.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
        }
        return response;
    }

    private static bool WasReservedCounterUsed(ReceiptResponse response, string series, long aa)
    {
        // Commit only if AADE confirmed the submission *and* it used our reservation.
        // MyDataSCU rewrites the country segment after "#" with the (series, aa) that
        // actually went to AADE on a successful submission. Handwritten documents are
        // taken inbound before a reservation is ever made, so the only remaining path
        // that can legitimately produce a different segment is a mydataoverride
        // carrying its own Series/Aa — those documents are caller-numbered too and must
        // not advance the counter.
        if (!response.ftState.IsState(State.Success))
        {
            return false;
        }
        return CountrySegment.TryParse(response.ftReceiptIdentification, out var actualSeries, out var actualAa)
            && string.Equals(actualSeries, series, StringComparison.Ordinal)
            && actualAa == aa;
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
