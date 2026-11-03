using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;
using ScuSignatureTypeFR = fiskaltrust.Middleware.SCU.FR.Abstraction.Cases.SignatureTypeFR;

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

    // Counting the earlier copies, signing and recording the new one must not interleave with
    // another copy, or two copies of the same receipt would get the same number.
    private readonly SemaphoreSlim _copyLock = new(1, 1);

    public ProtocolCommandProcessorFR(FRSigningPipeline pipeline, AsyncLazy<IJournalFRCopyPayloadRepository> copies)
    {
        _pipeline = pipeline;
        _copies = copies;
    }

    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>Orders are internal working steps and never leave the till as a document.</summary>
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    /// <summary>
    /// A copy is numbered per copied receipt (R13 ENC-DUP-PRN-NUM, NF525 V2.3 §6.3.1.3; market-fr#235).
    /// The count comes from the copy journal ftJournalFRCopyPayload, one row per issued copy, which
    /// v1.2 writes too - so a half-migrated queue keeps counting the copies v1 issued. The number is
    /// signed by the SCU and printed as "n. Duplicata de reference", as in v1.2.
    /// </summary>
    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        // ReceiptValidatorFR has made sure there is exactly one reference.
        var copiedReceiptReference = request.ReceiptRequest.cbPreviousReceiptReference!.SingleValue!;

        // A training copy is not a fiscal copy: it neither reads nor feeds the copy journal, so it
        // cannot shift the numbering of the real copies (as v1's CopyCommand).
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateDuplicateSignature(copiedReceiptReference, copyNumber: null));
            return await _pipeline.SignAsync(request).ConfigureAwait(false);
        }

        var copies = await _copies;

        await _copyLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var copyNumber = await copies.GetCountOfCopiesAsync(copiedReceiptReference).ConfigureAwait(false) + 1;
            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateDuplicateSignature(copiedReceiptReference, copyNumber));

            // The copy is journaled before the chain advances: if that fails, the copy is not
            // issued and its number is not consumed, so no reprint number is ever signed twice.
            return await _pipeline.SignAsync(request, copyNumber: copyNumber, recordSigned: signed => copies.InsertAsync(CopyPayload(signed, copiedReceiptReference))).ConfigureAwait(false);
        }
        finally
        {
            _copyLock.Release();
        }
    }

    private static ftJournalFRCopyPayload CopyPayload(ReceiptResponse response, string copiedReceiptReference) => new()
    {
        QueueId = response.ftQueueID,
        QueueItemId = response.ftQueueItemID,
        CashBoxIdentification = response.ftCashBoxIdentification,
        ReceiptId = response.ftReceiptIdentification,
        ReceiptMoment = response.ftReceiptMoment,
        CopiedReceiptReference = copiedReceiptReference,
        Siret = SignatureData(response, ScuSignatureTypeFR.Siret),
        CertificateSerialNumber = SignatureData(response, ScuSignatureTypeFR.CertificateSerialNumber),
    };

    /// <summary>The SIRET and certificate items are written by the SCU, so their types are the SCU's.</summary>
    private static string? SignatureData(ReceiptResponse response, ScuSignatureTypeFR type)
        => response.ftSignatures?.FirstOrDefault(x => ((long) x.ftSignatureType & 0xFFFF) == ((long) type & 0xFFFF))?.Data;
}
