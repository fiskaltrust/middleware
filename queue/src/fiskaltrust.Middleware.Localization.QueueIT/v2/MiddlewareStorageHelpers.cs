using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public static class MiddlewareStorageHelpers
{
    public static async Task<ReceiptResponse> LoadReceiptReferencesToResponse(
        IMiddlewareQueueItemRepository queueItemRepository,
        ReceiptRequest request,
        DateTime receiptMomentFallback,
        ReceiptResponse receiptResponse)
    {
        var cbPreviousReceiptReference = request.cbPreviousReceiptReference?.SingleValue;
        if (string.IsNullOrEmpty(cbPreviousReceiptReference))
        {
            receiptResponse.SetReceiptResponseError("No previous receipt reference was provided.");
            return receiptResponse;
        }

        var queueItems = queueItemRepository.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null);
        if (queueItems == null || !await queueItems.AnyAsync())
        {
            receiptResponse.SetReceiptResponseError($"There is no item available with the given cbPreviousReceiptReference '{cbPreviousReceiptReference}'.");
            return receiptResponse;
        }

        if (!string.IsNullOrEmpty(request.cbTerminalID) && await queueItems.AnyAsync(x => x.cbTerminalID == request.cbTerminalID))
        {
            queueItems = queueItems.Where(x => x.cbTerminalID == request.cbTerminalID);
        }

        await foreach (var existingQueueItem in queueItems)
        {
            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            if (referencedResponse is null) continue;

            if (referencedResponse.ftState.IsState(State.Error))
            {
                continue;
            }
            if (referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber) == null
                || referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber) == null
                || referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment) == null)
            {
                break;
            }

            var documentNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber)!.Data;
            var zNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data;
            var documentMoment = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data
                                 ?? receiptMomentFallback.ToString("yyyy-MM-dd");

            receiptResponse.ftSignatures.AddRange(new[]
            {
                new SignatureItem
                {
                    Caption = "<reference-z-number>",
                    Data = zNumber.ToString(),
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber),
                },
                new SignatureItem
                {
                    Caption = "<reference-doc-number>",
                    Data = documentNumber.ToString(),
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber),
                },
                new SignatureItem
                {
                    Caption = "<reference-timestamp>",
                    Data = documentMoment,
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment),
                },
            });
            break;
        }
        return receiptResponse;
    }
}
