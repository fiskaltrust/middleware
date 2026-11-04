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
/// The log cases feed the French technical event and accounting log ("journal des evenements"),
/// which NF525 requires to be signed and chained just like the sales chains - chain "L". Copies of
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

    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>Orders are internal working steps and never leave the till as a document.</summary>
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

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
    /// The earlier copies of the reference that were issued: a targeted lookup (the journal rows of
    /// this reference, then each one's queue item by ID), never a scan of the queue items.
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

            var queueItem = await queueItems.GetAsync(copy.QueueItemId).ConfigureAwait(false);
            if (WasIssued(queueItem))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// A copy counts unless its queue item shows it was not issued: no response yet (the process
    /// stopped while signing it) or an error state. A row whose queue item cannot be read is a v1.2
    /// row - v1.2 journals a copy only once it is signed - and counts: counting it at worst leaves a
    /// gap, not counting it could print the same number twice.
    /// </summary>
    private static bool WasIssued(ftQueueItem? queueItem)
    {
        if (queueItem is null)
        {
            return true;
        }

        if (string.IsNullOrEmpty(queueItem.response))
        {
            return false;
        }

        try
        {
            var outcome = JsonSerializer.Deserialize<ResponseState>(queueItem.response);
            return outcome is null || !outcome.ftState.IsState(State.Error);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    /// <summary>
    /// The journal row, written before signing: it links the copied reference to the copy's queue
    /// item. What the SCU signs (receipt ID, SIRET, certificate) is on that queue item's response, so
    /// those columns - NOT NULL in the v1.2 schema - stay empty.
    /// </summary>
    private static ftJournalFRCopyPayload CopyRecord(ReceiptResponse response, string copiedReceiptReference) => new()
    {
        QueueId = response.ftQueueID,
        QueueItemId = response.ftQueueItemID,
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
