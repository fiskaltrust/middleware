using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ReceiptReferenceProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;
    public ReceiptReferenceProvider(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    public async Task<List<(ReceiptRequest, ReceiptResponse)>> GetReceiptReferencesAsync(ProcessCommandRequest request)
    {
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
        }
        return receiptReferences;
    }

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

    private async Task<(ReceiptRequest, ReceiptResponse)> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse, string cbPreviousReceiptReferenceString)
    {
        var queueItems = (await _readOnlyQueueItemRepository).GetByReceiptReferenceAsync(cbPreviousReceiptReferenceString, request.cbTerminalID);
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