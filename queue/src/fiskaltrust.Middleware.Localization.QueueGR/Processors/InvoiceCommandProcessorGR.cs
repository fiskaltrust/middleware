using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Logic;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class InvoiceCommandProcessorGR(IGRSSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
#pragma warning restore

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request)
    {
        var receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, receiptReferences);
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);
}