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

        // Commit gate: AADE's invoiceMark is the proof that an invoice was actually
        // filed. The SCU may legitimately answer Success without submitting one —
        // delivery-note cancellations and Pay0x3005 payment methods go to different
        // AADE endpoints and never consume an aa — so Success alone must not advance
        // the counter. When a mark IS present, the filed numbering is exactly the
        // reserved one: the queue is the single writer of the country segment
        // (handwritten numbering is taken inbound, series/aa overrides are rejected)
        // and AADEFactory derives the document numbering from that segment.
        var mark = TryExtractMark(response.ReceiptResponse);
        if (response.ReceiptResponse.ftState.IsState(State.Success) && mark != null)
        {
            queueGR.InvoiceNumerator = reservedAa;
            queueGR.LastInvoiceMoment = request.ReceiptRequest.cbReceiptMoment;
            queueGR.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueGR.LastInvoiceMark = mark;
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
        // AADEFactory derives the document numbering from this segment; nothing rewrites
        // it afterwards — the queue is its single writer.
        var originalReceiptIdentification = request.ReceiptResponse.ftReceiptIdentification;
        request.ReceiptResponse.ftReceiptIdentification += $"{series}-{aa}";

        ProcessResponse response;
        try
        {
            response = await sscdCall();
        }
        catch
        {
            // The SCU call failed outright, so no invoice was filed. Failed receipts
            // must persist exactly the identification they had before this feature
            // existed ("ft{N:X}#") — restore it before the exception reaches
            // SignProcessor, which persists the response as failed.
            request.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
            throw;
        }

        if (!response.ReceiptResponse.ftState.IsState(State.Success) || TryExtractMark(response.ReceiptResponse) == null)
        {
            // Keep the segment only when AADE actually filed an invoice (Success plus
            // invoiceMark). Failed submissions and SCU flows that never file one
            // (delivery-note cancellation, payment methods) must persist exactly the
            // identification they had before: the segment is the durable marker that
            // this (series, aa) was consumed at AADE, and the queue-start migration
            // seeds from it.
            response.ReceiptResponse.ftReceiptIdentification = originalReceiptIdentification;
        }
        return response;
    }

    private static long? TryExtractMark(ReceiptResponse response)
    {
        // AADE's invoiceMark as stamped by the SendInvoices success path, which types it
        // as SignatureTypeGR.Mark. The SCU's non-invoice flows (delivery-note
        // cancellation, payment methods) type all their response items as
        // GenericMyDataInfo, so they can never satisfy this lookup — even if one of
        // their items happens to be captioned "invoiceMark".
        var markSignature = response.ftSignatures?
            .FirstOrDefault(s => string.Equals(s.Caption, "invoiceMark", StringComparison.Ordinal)
                && s.ftSignatureType.IsType(SignatureTypeGR.Mark));
        return markSignature != null && long.TryParse(markSignature.Data, out var mark)
            ? mark
            : (long?) null;
    }
}
