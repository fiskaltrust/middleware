using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Scu;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public class ReceiptCommandProcessorIT : IReceiptCommandProcessor
{
    private readonly IITSSCD _itSSCD;
    private readonly IJournalITRepository _journalITRepository;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;
    private readonly ftQueueIT _queueIT;

    public ReceiptCommandProcessorIT(
        IITSSCD itSSCD,
        IJournalITRepository journalITRepository,
        IMiddlewareQueueItemRepository queueItemRepository,
        ftQueueIT queueIT)
    {
        _itSSCD = itSSCD;
        _journalITRepository = journalITRepository;
        _queueItemRepository = queueItemRepository;
        _queueIT = queueIT;
    }

    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request)
        => PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase;
        var isVoidOrRefund = receiptCase.IsFlag(ReceiptCaseFlags.Void) || receiptCase.IsFlag(ReceiptCaseFlags.Refund);
        if (isVoidOrRefund && request.ReceiptRequest.cbPreviousReceiptReference is { SingleValue: { Length: > 0 } })
        {
            await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(_queueItemRepository, request.ReceiptRequest, request.ReceiptRequest.cbReceiptMoment, request.ReceiptResponse);
            if (request.ReceiptResponse.ftState.IsState(State.Error))
            {
                return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
            }
        }

        var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
        var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
        V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
        if (request.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        var documentNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)!.Data;
        var zNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data;
        if (!request.ReceiptResponse.ftReceiptIdentification.EndsWith("#"))
        {
            // SCU already produced the final identification; nothing to append.
        }
        else
        {
            request.ReceiptResponse.ftReceiptIdentification += $"{zNumber.PadLeft(4, '0')}-{documentNumber.PadLeft(4, '0')}";
        }

        request.ReceiptResponse.InsertSignatureItems(SignaturItemFactory.CreatePOSReceiptFormatSignatures(request.ReceiptResponse));

        var journalIT = ftJournalITFactory.CreateFrom(
            request.ReceiptResponse.ftQueueItemID,
            request.ReceiptRequest.cbReceiptReference,
            _queueIT,
            new ScuResponse
            {
                ftReceiptCase = (long) request.ReceiptRequest.ftReceiptCase,
                ReceiptNumber = long.Parse(documentNumber),
                ZRepNumber = long.Parse(zNumber),
            });
        await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
        return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
    }

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request)
        => PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
}
