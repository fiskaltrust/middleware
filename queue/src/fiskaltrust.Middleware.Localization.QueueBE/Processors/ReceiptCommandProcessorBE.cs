using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class ReceiptCommandProcessorBE(IBESSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IBESSCD _sscd = sscd;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);
}