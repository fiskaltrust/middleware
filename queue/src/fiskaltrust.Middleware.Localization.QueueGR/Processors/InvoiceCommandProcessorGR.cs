using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class InvoiceCommandProcessorGR(IGRSSCD sscd, ftQueueGR queueGR, ftSignaturCreationUnitGR signaturCreationUnitGR, IMiddlewareQueueItemRepository readOnlyQueueItemRepository) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly ftQueueGR _queueGR = queueGR;
    private readonly ftSignaturCreationUnitGR _signaturCreationUnitGR = signaturCreationUnitGR;
    private readonly IMiddlewareQueueItemRepository _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) && !string.IsNullOrEmpty(request.ReceiptRequest.cbPreviousReceiptReference))
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = receiptReference,
            });
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            });
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
    }

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    private async Task<ReceiptResponse> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var queueItems = _readOnlyQueueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            if (referencedResponse != null)
            {
                receiptResponse.ftStateData = new
                {
                    ReferencedReceiptResponse = referencedResponse
                };
                return referencedResponse;
            }
            else
            {
                throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{request.cbPreviousReceiptReference}' sent via the request.");
            }
        }
        throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{request.cbPreviousReceiptReference}' sent via the request.");
    }
}