using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.v2.Validation;

public class ReceiptReferenceProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;
    public ReceiptReferenceProvider(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    public async Task<bool> HasExistingRefundAsync(string cbPreviousReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;

        var lastItem = await queueItemRepository.GetLastQueueItemAsync();
        if (lastItem == null)
        {
            return false;
        }

        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }

            try
            {
                var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (referencedRequest != null && referencedResponse != null &&
                    referencedRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) &&
                    referencedRequest.cbPreviousReceiptReference != null
                    && referencedResponse.ftState.IsState(State.Success))
                {
                    var previousRef = referencedRequest.cbPreviousReceiptReference.SingleValue;
                    if (previousRef == cbPreviousReceiptReference)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return false;
    }

    public async Task<bool> HasExistingVoidAsync(string cbPreviousReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;

        var lastItem = await queueItemRepository.GetLastQueueItemAsync();
        if (lastItem == null)
        {
            return false;
        }

        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request) || string.IsNullOrEmpty(queueItem.response))
            {
                continue;
            }

            try
            {
                var referencedRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (referencedRequest != null && referencedResponse != null &&
                    referencedRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) &&
                    referencedRequest.cbPreviousReceiptReference != null
                    && referencedResponse.ftState.IsState(State.Success))
                {
                    var previousRef = referencedRequest.cbPreviousReceiptReference.SingleValue;
                    if (previousRef == cbPreviousReceiptReference)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return false;
    }
}
