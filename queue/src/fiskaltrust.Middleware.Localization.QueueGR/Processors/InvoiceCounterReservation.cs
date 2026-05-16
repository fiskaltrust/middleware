using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
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

        // One-time migration for queues that were activated before this code shipped.
        // Pre-upgrade, the aa transmitted to AADE was derived from the generic
        // ftReceiptNumerator. We seed InvoiceNumerator from that value so the first
        // post-upgrade aa is strictly greater than any aa AADE already has on file
        // for this queue, avoiding collisions on the deterministic uid. The gap
        // produced (we overshoot by the count of NoOp receipts that previously
        // advanced ftReceiptNumerator without submitting) is acceptable — AADE
        // permits non-contiguous aa, it just refuses duplicates.
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

        AttachProposal(request.ReceiptResponse, reservedSeries, reservedAa);

        var response = await sscdCall();

        if (WasReservedCounterUsed(response.ReceiptResponse, reservedSeries, reservedAa))
        {
            queueGR.InvoiceSeries = reservedSeries;
            queueGR.InvoiceNumerator = reservedAa;
            queueGR.LastInvoiceMoment = request.ReceiptRequest.cbReceiptMoment;
            queueGR.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueGR.LastInvoiceMark = TryExtractMark(response.ReceiptResponse);
            await configRepo.InsertOrUpdateQueueGRAsync(queueGR);
        }

        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    private static void AttachProposal(ReceiptResponse response, string series, long aa)
    {
        var state = response.ftStateData as MiddlewareSCUGRMyDataState ?? new MiddlewareSCUGRMyDataState();
        state.GR ??= new MiddlewareQueueGRState();
        state.GR.ProposedInvoiceCounter = new ProposedInvoiceCounter
        {
            Series = series,
            Aa = aa,
        };
        response.ftStateData = state;
    }

    private static bool WasReservedCounterUsed(ReceiptResponse response, string series, long aa)
    {
        // Commit only if AADE confirmed the submission *and* it used our reservation.
        // The MyDataSCU appends "{series}-{aa}" to ftReceiptIdentification only on a
        // successful SendInvoices call (line 300 of MyDataSCU.cs), using the values
        // that actually went into the doc. If the request was overridden by a
        // handwritten payload or mydataoverride, those values won't equal ours.
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
