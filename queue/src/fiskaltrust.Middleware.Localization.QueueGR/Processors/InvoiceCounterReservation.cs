using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

internal static class InvoiceCounterReservation
{
    /// <summary>
    /// AADE rejects a submission whose (issuer, series, aa) is already filed with
    /// validation error 233. The queue is the single writer of its own series
    /// (handwritten numbering into the own series and series/aa overrides are both
    /// rejected), so a 233 on a reservation can only mean the counter is behind AADE —
    /// the reserved number is consumed.
    /// </summary>
    private const string DuplicateAaAadeErrorCode = "233";

    public static async Task<ProcessCommandResponse> InvokeWithCounterAsync(
        ProcessCommandRequest request,
        AsyncLazy<IConfigurationRepository> configurationRepository,
        IQueueStorageProvider queueStorageProvider,
        ILogger logger,
        Func<Task<ProcessResponse>> sscdCall)
    {
        var configRepo = await configurationRepository;
        var queueGR = await configRepo.GetQueueGRAsync(request.queue.ftQueueId);

        // Handwritten documents are caller-numbered: the merchant already stamped
        // (series, aa) on the paper original, so the queue takes them inbound verbatim.
        // No reservation is made and the counter is never read or written. Full
        // validation of the handwritten payload stays in the SCU — except for one
        // queue-level rule: the handwritten series must not be the queue's own invoice
        // series, otherwise the caller could file numbers the counter doesn't know
        // about and a later automatic reservation would collide at AADE.
        if (TryGetHandwrittenNumbering(request.ReceiptRequest, out var handwrittenSeries, out var handwrittenAa))
        {
            if (string.Equals(handwrittenSeries, queueGR.InvoiceSeries, StringComparison.Ordinal))
            {
                request.ReceiptResponse.SetReceiptResponseError(
                    $"The handwritten Series '{handwrittenSeries}' equals the queue's own invoice series. Numbering in this series is assigned exclusively by the middleware — use a dedicated series for handwritten documents.");
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }
            var handwrittenResponse = await SubmitWithSegmentAsync(request, handwrittenSeries, handwrittenAa, sscdCall);
            return new ProcessCommandResponse(handwrittenResponse.ReceiptResponse, []);
        }

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

        if (IsInvoiceFilingCase(request.ReceiptRequest) && IsDuplicateAaError(response.ReceiptResponse))
        {
            // "Number consumed, advance": AADE proved the reserved aa is already filed,
            // so move the persisted counter past it — and nothing else. This receipt
            // still fails (no automatic resubmission; the POS retry loop is the retry
            // mechanism), but the next attempt reserves a fresh number instead of
            // re-reserving the same one forever. This heals both known directions one
            // number per submission: the commit write below failing after AADE filed
            // the invoice, and a queue-start seed below historical out-of-order values
            // (see InvoiceCounterMigration). Handwritten documents never get here —
            // they returned above, before the reservation. Their numbering is
            // caller-owned, so a handwritten 233 is the caller's duplicate to fix and
            // must never move the queue's counter.
            queueGR.InvoiceNumerator = reservedAa;
            await configRepo.InsertOrUpdateQueueGRAsync(queueGR);
            // Two sinks on purpose: the action journal is the durable per-queue audit
            // trail, the structured warning is what OpenTelemetry/AppInsights pick up
            // for alerting across queues.
            await queueStorageProvider.CreateActionJournalAsync(
                $"AADE rejected aa {reservedAa} in series '{reservedSeries}' as a duplicate (233) — the invoice counter was behind AADE. The counter advanced to {reservedAa}; the next submission reserves aa {reservedAa + 1}.",
                $"{response.ReceiptResponse.ftState:X}",
                response.ReceiptResponse.ftQueueItemID);
            logger.LogWarning(
                "AADE rejected aa {RejectedAa} in series '{InvoiceSeries}' as a duplicate (233) for queue {QueueId} (queue item {QueueItemId}) — the invoice counter was behind AADE. Advanced InvoiceNumerator to {InvoiceNumerator}; the next submission reserves aa {NextAa}.",
                reservedAa, reservedSeries, request.queue.ftQueueId, response.ReceiptResponse.ftQueueItemID, queueGR.InvoiceNumerator, queueGR.InvoiceNumerator + 1);
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }

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
            queueGR.LastInvoiceMoment = response.ReceiptResponse.ftReceiptMoment;
            queueGR.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueGR.LastInvoiceMark = mark;
            // Accepted-risk window (shared with the ES processors): if this write fails
            // after AADE filed the invoice, the aa is consumed at AADE while the counter
            // stays behind — the retry re-reserves it, AADE answers 233 once, and the
            // duplicate-aa advance above moves the counter past it.
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
        // AADEFactory derives the document numbering from this segment; on success
        // MyDataSCU writes the filed values back into it — an identity operation here,
        // since the doc numbering comes from this very segment.
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

    private static bool IsInvoiceFilingCase(ReceiptRequest request)
    {
        // The advance reads a 233 as "this aa is consumed at AADE", which is only
        // meaningful for submissions the SCU routes to SendInvoices. Pay0x3005 and
        // voided delivery notes go to the payment-methods / cancellation endpoints
        // instead: their errors never describe the queue's invoice numbering, and
        // their reservations are never consumed on success either (no mark) — so
        // advancing on a 233-shaped failure there would burn a number for good.
        // Better to skip a heal than to skip a number: anything that is not certainly
        // an invoice submission fails without touching the counter.
        if (request.ftReceiptCase.IsCase(ReceiptCase.Pay0x3005))
        {
            return false;
        }
        if (request.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005) && request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            return false;
        }
        return true;
    }

    private static bool IsDuplicateAaError(ReceiptResponse response)
    {
        if (!response.ftState.IsState(State.Error))
        {
            return false;
        }
        // MyDataSCU surfaces AADE rejections as a FAILURE signature whose Data is the
        // serialized AADEEErrorResponse: {"AADEError":"...","Errors":[{"message":"...",
        // "code":"..."}]}. The property names duplicate scu-gr's AADEEErrorResponse and
        // the xsd-generated ErrorType — if either side drifts, the advance silently
        // stops triggering and a 233 fails the receipt without moving the counter,
        // exactly as it did before the heal existed.
        foreach (var signature in response.ftSignatures ?? [])
        {
            if (!string.Equals(signature.Caption, "FAILURE", StringComparison.Ordinal) || string.IsNullOrEmpty(signature.Data))
            {
                continue;
            }
            try
            {
                using var document = JsonDocument.Parse(signature.Data);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("Errors", out var errors)
                    || errors.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (var error in errors.EnumerateArray())
                {
                    if (error.ValueKind == JsonValueKind.Object
                        && error.TryGetProperty("code", out var code)
                        && code.ValueKind == JsonValueKind.String
                        && string.Equals(code.GetString(), DuplicateAaAadeErrorCode, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Not every FAILURE carries the AADE error JSON (mapping and transport
                // errors are plain text) — those are never duplicate-aa rejections.
            }
        }
        return false;
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
        return markSignature != null && long.TryParse(markSignature.Data, NumberStyles.None, CultureInfo.InvariantCulture, out var mark)
            ? mark
            : (long?) null;
    }
}
