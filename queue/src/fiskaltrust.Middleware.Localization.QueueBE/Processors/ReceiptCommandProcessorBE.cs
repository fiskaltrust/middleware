using fiskaltrust.ifPOS.v2;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class ReceiptCommandProcessorBE(IBESSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IBESSCD _sscd = sscd;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, receiptReferences);
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request)
    {
        var receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, receiptReferences);
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => BEFallBackOperations.NotSupported(request, "PointOfSaleReceiptWithoutObligation");

    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => BEFallBackOperations.NotSupported(request, "ECommerce");

    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => BEFallBackOperations.NotSupported(request, "DeliveryNote");
}