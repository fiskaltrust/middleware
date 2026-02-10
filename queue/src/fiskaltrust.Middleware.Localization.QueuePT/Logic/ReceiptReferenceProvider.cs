using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using static fiskaltrust.Middleware.Localization.QueuePT.Logic.ReceiptReferenceProvider;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class ReceiptReferenceProvider
{
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository;

    public ReceiptReferenceProvider(AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository)
    {
        _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    }

    public async Task<bool> HasExistingElementByConditionAsync(string cbPreviousReceiptReference, Func<ReceiptRequest, bool> condition)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
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

                if (referencedRequest == null || referencedResponse == null)
                {
                    continue;
                }

                if (!referencedResponse.ftState.IsState(State.Success))
                {
                    continue;
                }

                if (referencedRequest.cbPreviousReceiptReference == null || referencedRequest.cbPreviousReceiptReference.SingleValue != cbPreviousReceiptReference)
                {
                    continue;
                }

                if (condition(referencedRequest))
                {
                    return true;
                }
            }
            catch
            {
                continue;
            }
        }
        return false;
    }

    public async Task<bool> HasExistingPaymentTransferAsync(string cbPreviousReceiptReference) => await HasExistingElementByConditionAsync(cbPreviousReceiptReference, request => request.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002));

    public async Task<bool> HasExistingRefundAsync(string cbPreviousReceiptReference) => await HasExistingElementByConditionAsync(cbPreviousReceiptReference, request => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund));

    public async Task<bool> HasExistingVoidAsync(string cbPreviousReceiptReference) => await HasExistingElementByConditionAsync(cbPreviousReceiptReference, request => request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void));

    /// <summary>
    /// Gets all partial refunds for a given receipt reference
    /// </summary>
    public async Task<List<ReceiptRequest>> GetExistingPartialRefundsAsync(string cbPreviousReceiptReference)
    {
        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        var partialRefunds = new List<ReceiptRequest>();

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

                if (referencedRequest == null || referencedResponse == null)
                {
                    continue;
                }

                if (!referencedResponse.ftState.IsState(State.Success))
                {
                    continue;
                }

                if (referencedRequest.cbPreviousReceiptReference == null || referencedRequest.cbPreviousReceiptReference.SingleValue != cbPreviousReceiptReference)
                {
                    continue;
                }

                if (referencedRequest.IsPartialRefundReceipt())
                {
                    partialRefunds.Add(referencedRequest);
                }
            }
            catch
            {
                continue;
            }
        }

        return partialRefunds;
    }

    public async Task<bool> HasExistingInvoiceForWorkingDocumentAsync(string cbPreviousReceiptReference)
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
                if (referencedRequest == null || referencedResponse == null)
                {
                    continue;
                }

                if (!referencedResponse.ftState.IsState(State.Success))
                {
                    continue;
                }

                if (!HasPreviousReceiptReference(referencedRequest, cbPreviousReceiptReference))
                {
                    continue;
                }

                if (IsInvoiceReceipt(referencedResponse))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore deserialization errors and continue
                continue;
            }
        }

        return false;
    }

    public async Task<List<ReceiptChargeItemMatch>> GetChargeItemMatchesForPreviousReferenceAsync(
        string cbPreviousReceiptReference,
        IEnumerable<ChargeItem> chargeItems)
    {
        if (string.IsNullOrWhiteSpace(cbPreviousReceiptReference) || chargeItems == null)
        {
            return [];
        }

        var queueItemRepository = await _readOnlyQueueItemRepository.Value;
        var results = new List<ReceiptChargeItemMatch>();
        var descriptionMatches = chargeItems
            .Select(item => item.Description)
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (descriptionMatches.Count == 0)
        {
            return results;
        }

        Receipt? referencedReceipt = null;

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
                if (referencedRequest == null || referencedResponse == null)
                {
                    continue;
                }

                if (!referencedResponse.ftState.IsState(State.Success))
                {
                    continue;
                }

                if (!string.Equals(referencedRequest.cbReceiptReference, cbPreviousReceiptReference, StringComparison.Ordinal))
                {
                    continue;
                }

                referencedReceipt = new Receipt
                {
                    Request = referencedRequest,
                    Response = referencedResponse
                };
                break;
            }
            catch
            {
                continue;
            }
        }

        if (referencedReceipt?.Request.cbChargeItems == null || referencedReceipt.Request.cbChargeItems.Count == 0)
        {
            return results;
        }

        foreach (var chargeItem in referencedReceipt.Request.cbChargeItems)
        {
            if (string.IsNullOrWhiteSpace(chargeItem.Description))
            {
                continue;
            }

            if (!descriptionMatches.Contains(chargeItem.Description))
            {
                continue;
            }

            results.Add(new ReceiptChargeItemMatch(referencedReceipt, chargeItem));
        }

        return results;
    }

    private static bool HasPreviousReceiptReference(ReceiptRequest request, string cbPreviousReceiptReference)
    {
        if (request.cbPreviousReceiptReference == null)
        {
            return false;
        }

        if (request.cbPreviousReceiptReference.IsSingle)
        {
            return request.cbPreviousReceiptReference.SingleValue == cbPreviousReceiptReference;
        }

        if (request.cbPreviousReceiptReference.IsGroup)
        {
            return request.cbPreviousReceiptReference.GroupValue.Contains(cbPreviousReceiptReference);
        }

        return false;
    }

    private static bool IsInvoiceReceipt(ReceiptResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.ftReceiptIdentification))
        {
            return false;
        }

        var idParts = response.ftReceiptIdentification.Split('#');
        var idTail = idParts.Length == 0 ? string.Empty : idParts[^1];
        var typeParts = idTail.Split(' ');
        var type = typeParts.Length == 0 ? string.Empty : typeParts[0];
        return type == "FT" || type == "FS";
    }

}

public sealed record ReceiptChargeItemMatch(Receipt ReferencedReceipt, ChargeItem ReferencedChargeItem);
