using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public class MiddlewareStorageHelpers
    {
        public static async Task<ReceiptResponse> LoadReceiptReferencesToResponse(IMiddlewareQueueItemRepository queueItemRepository, ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            var queueItems = queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
            if (queueItems == null || !await queueItems.AnyAsync())
            {
                receiptResponse.SetReceiptResponseError($"There is no item available with the given cbPreviousReceiptReference '{request.cbPreviousReceiptReference}'.");
                return receiptResponse;
            }

            await foreach (var existingQueueItem in queueItems)
            {
                var referencedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(existingQueueItem.response);
                var signatures = new List<SignaturItem>();
                signatures.AddRange(receiptResponse.ftSignatures);
                foreach (var signature in referencedResponse.ftSignatures)
                {
                    if (signature.ftSignatureFormat != (long) SignaturItem.Formats.Text)
                    {
                        continue;
                    }

                    if (signature.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber))
                    {
                        signatures.Add(new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = signature.Data.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        });
                    }
                    else if (signature.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber))
                    {
                        signatures.Add(new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = signature.Data.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        });
                    }
                    else if (signature.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment))
                    {
                        var documentMoment = signature.Data;
                        documentMoment ??= queueItem.cbReceiptMoment.ToString("yyyy-MM-dd");
                        signatures.Add(new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = documentMoment,
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        });
                    }
                }
                receiptResponse.ftSignatures = signatures.ToArray();
                break;
            }
            return receiptResponse;
        }
    }
}