using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.be;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class ReceiptCommandProcessorBE(IBESSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IBESSCD _sscd = sscd;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(queueItemRepository);
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository = queueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var queueItemRepository = await _queueItemRepository;
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);
}