using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
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

    public async Task<ReceiptRequest?> LoadOriginalReceiptAsync(string cbPreviousReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        var queueItems = await queueItemRepository.GetByReceiptReferenceAsync(cbPreviousReceiptReference).ToListAsync();
        var finished = queueItems
            .Where(qi => qi.IsReceiptRequestFinished())
            .Select(qi => new
            {
                Request = JsonSerializer.Deserialize<ReceiptRequest>(qi.request),
                Response = JsonSerializer.Deserialize<ReceiptResponse>(qi.response)
            })
            .Where(x => x.Request != null && x.Response != null && !x.Response!.ftState.IsState(State.Error))
            .ToList();

        return finished.Count == 1 ? finished[0].Request : null;
    }

    public async Task<List<ReceiptRequest>> LoadExistingPartialRefundsAsync(string cbPreviousReceiptReference, string currentCbReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        var existingRefunds = new List<ReceiptRequest>();

        await foreach (var queueItem in queueItemRepository.GetEntriesOnOrAfterTimeStampAsync(0))
        {
            if (string.IsNullOrEmpty(queueItem.request))
            {
                continue;
            }

            try
            {
                var request = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                if (request != null && request.cbPreviousReceiptReference != null
                    && request.cbReceiptReference != currentCbReceiptReference
                    && request.IsPartialRefundReceipt()
                    && request.cbPreviousReceiptReference.SingleValue == cbPreviousReceiptReference)
                {
                    existingRefunds.Add(request);
                }
            }
            catch
            {
                continue;
            }
        }

        return existingRefunds;
    }

    public async Task<bool> HasExistingPaymentTransferAsync(string cbPreviousReceiptReference)
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
                    referencedRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) &&
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
