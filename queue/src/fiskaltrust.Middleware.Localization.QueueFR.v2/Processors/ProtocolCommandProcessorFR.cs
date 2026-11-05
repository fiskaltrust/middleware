using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The technical and accounting events feed the French event log ("journal des evenements"),
/// which NF525 requires to be signed and chained just like the sales chains - chain "L". As in
/// v1.2, an unspecified protocol and an internal consumption are stored but not signed, an order
/// is signed as a bill and a pay protocol as a payment proof. Copies of
/// an already issued document go into the duplicate chain ("D") and must be marked as a duplicata.
/// </summary>
public class ProtocolCommandProcessorFR : IProtocolCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;
    private readonly AsyncLazy<IJournalFRCopyPayloadRepository> _copies;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItems;

    public ProtocolCommandProcessorFR(FRSigningPipeline pipeline, AsyncLazy<IJournalFRCopyPayloadRepository> copies, AsyncLazy<IMiddlewareQueueItemRepository> queueItems)
    {
        _pipeline = pipeline;
        _copies = copies;
        _queueItems = queueItems;
    }

    /// <summary>v1.2 does not sign an unspecified protocol (v0 0x14).</summary>
    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>v1.2 does not sign an internal consumption (v0 0x0D).</summary>
    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    /// <summary>v1.2 signs an order as a bill (v0 0x08) in the bill chain, with the provisional-document mention.</summary>
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateProvisionalDocumentSignature());
        return _pipeline.SignAsync(request);
    }

    /// <summary>v1.2 signs a pay protocol as a payment proof (v0 0x02) in the payment-proof chain.</summary>
    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePaymentProofSignature());
        return _pipeline.SignAsync(request);
    }

    /// <summary>
    /// A copy is numbered per copied receipt (R13 ENC-DUP-PRN-NUM, NF525 V2.3 §6.3.1.3; market-fr#235)
    /// and printed as "n. Duplicata de reference", as in v1.2. The copy is journaled in
    /// ftJournalFRCopyPayload first, then numbered from the copies of the same receipt that were
    /// signed, then signed: the number reaches every FR SCU in the FR block of ftStateData.
    /// <para>
    /// The journal row only links the reference to the copy's queue item; whether a copy was issued
    /// is read from that queue item's response. A copy whose signing failed (or that never finished)
    /// keeps its row but is never counted, so nothing has to be rolled back and the retry - a new
    /// queue item - gets the number the failed copy did not consume. Requests are processed one at a
    /// time per queue (LocalQueueSynchronizationDecorator), so two copies cannot interleave.
    /// </para>
    /// </summary>
    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        // ReceiptValidatorFR has made sure there is exactly one reference.
        var copiedReceiptReference = request.ReceiptRequest.cbPreviousReceiptReference!.SingleValue!;

        // A training copy is not a fiscal copy: it neither feeds nor reads the copy journal, so it
        // cannot shift the numbering of the real copies (as v1's CopyCommand).
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateDuplicateSignature(copiedReceiptReference, copyNumber: null));
            return await _pipeline.SignAsync(request).ConfigureAwait(false);
        }

        var copies = await _copies;
        await copies.InsertAsync(CopyRecord(request.ReceiptResponse, copiedReceiptReference)).ConfigureAwait(false);

        var copyNumber = await CountSignedCopiesAsync(copies, copiedReceiptReference, request.ReceiptResponse.ftQueueItemID).ConfigureAwait(false) + 1;

        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateDuplicateSignature(copiedReceiptReference, copyNumber));
        var stateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.CopyNumber = copyNumber;
        request.ReceiptResponse.ftStateData = stateData;

        return await _pipeline.SignAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// The earlier copies of the reference that took a number. Every read is targeted: the journal
    /// rows of this reference, then, for a v2 row only, its queue item by ID and queue row - a point
    /// read in every storage, Azure included, where queue items are partitioned by row.
    /// </summary>
    private async Task<int> CountSignedCopiesAsync(IJournalFRCopyPayloadRepository copies, string copiedReceiptReference, Guid currentQueueItemId)
    {
        var queueItems = await _queueItems;
        var count = 0;
        foreach (var copy in await copies.GetCopiesAsync(copiedReceiptReference).ConfigureAwait(false))
        {
            if (copy.QueueItemId == currentQueueItemId)
            {
                continue;
            }

            // A v1.2 row counts whatever became of its copy: v1.2 counts every row of the reference
            // and journals a copy before signing it, so its numbering already consumed the numbers of
            // its unanswered copies, and v2 continues after the highest number v1.2 printed. v1.2
            // (and its backfill from history) never writes QueueRow.
            if (copy.QueueRow is not { } queueRow)
            {
                count++;
                continue;
            }

            if (WasIssued(await queueItems.GetAsync(copy.QueueItemId, queueRow).ConfigureAwait(false)))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// A v2 copy took a number only if it was issued: its queue item has a response (else the process
    /// stopped while signing it) and that response is not an error. The queue item is stored before
    /// the journal row, so a missing one was never answered either.
    /// </summary>
    private static bool WasIssued(ftQueueItem? queueItem)
    {
        if (string.IsNullOrEmpty(queueItem?.response))
        {
            return false;
        }

        try
        {
            var outcome = JsonSerializer.Deserialize<ResponseState>(queueItem!.response);
            return outcome is null || !outcome.ftState.IsState(State.Error);
        }
        catch (JsonException)
        {
            // Counting a copy that was not issued leaves a gap; not counting one that was would
            // print its number twice.
            return true;
        }
    }

    /// <summary>
    /// The journal row, written before signing: it links the copied reference to the copy's queue
    /// item and queue row. What the SCU signs (receipt ID, SIRET, certificate) is on that queue
    /// item's response, so those columns - NOT NULL in the v1.2 schema - stay empty.
    /// </summary>
    private static ftJournalFRCopyPayload CopyRecord(ReceiptResponse response, string copiedReceiptReference) => new()
    {
        QueueId = response.ftQueueID,
        QueueItemId = response.ftQueueItemID,
        QueueRow = response.ftQueueRow,
        CashBoxIdentification = response.ftCashBoxIdentification,
        ReceiptId = "",
        ReceiptMoment = response.ftReceiptMoment,
        CopiedReceiptReference = copiedReceiptReference,
        Siret = "",
        CertificateSerialNumber = "",
    };

    private sealed class ResponseState
    {
        public State ftState { get; set; }
    }
}
