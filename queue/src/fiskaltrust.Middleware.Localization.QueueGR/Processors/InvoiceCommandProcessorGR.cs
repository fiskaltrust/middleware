using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.ifPOS.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;

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
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) && request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            var receiptReferences = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, receiptReferences);
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

    private async Task<List<(ReceiptRequest, ReceiptResponse)>> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        if (request.cbPreviousReceiptReference is null)
        {
            return new List<(ReceiptRequest, ReceiptResponse)>();
        }

        return await request.cbPreviousReceiptReference.MatchAsync(
            async single => [await LoadReceiptReferencesToResponse(request, receiptResponse, single)],
            async group => {
                var references = new List<(ReceiptRequest, ReceiptResponse)>();
                foreach (var reference in group)
                {
                    var item = await LoadReceiptReferencesToResponse(request, receiptResponse, reference);
                    references.Add(item);
                }
                return references;
            }
        );

    }

#pragma warning disable
    private async Task<(ReceiptRequest, ReceiptResponse)> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse, string cbPreviousReceiptReferenceString)
    {
        var queueItems = _readOnlyQueueItemRepository.GetByReceiptReferenceAsync(cbPreviousReceiptReferenceString, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(existingQueueItem.request);
            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            if (referencedResponse != null && referencedRequest != null)
            {

                return (referencedRequest, referencedResponse);
            }
            else
            {
                throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
            }
        }
        throw new Exception($"Could not find a reference for the cbPreviousReceiptReference '{cbPreviousReceiptReferenceString}' sent via the request.");
    }
}