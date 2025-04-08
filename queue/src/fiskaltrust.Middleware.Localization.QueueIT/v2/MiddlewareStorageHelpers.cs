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
            var queueItems = queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, null);
            if (queueItems == null || !await queueItems.AnyAsync())
            {
                receiptResponse.SetReceiptResponseError($"There is no item available with the given cbPreviousReceiptReference '{request.cbPreviousReceiptReference}'.");
                return receiptResponse;
            }

            if (!string.IsNullOrEmpty(request.cbTerminalID) && await queueItems.AnyAsync(x => x.cbTerminalID == request.cbTerminalID))
            {
                queueItems = queueItems.Where(x => x.cbTerminalID == request.cbTerminalID);
            }


            await foreach (var existingQueueItem in queueItems)
            {  
                var referencedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(existingQueueItem.response);
                if ((referencedResponse.ftState & 0xEEEE_EEEE) == 0xEEEE_EEEE)
                {
                    continue;
                }
                if (referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber) == null || referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber) == null || referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment) == null)
                {
                    break;
                }



                var documentNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber).Data;
                var zNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
                var documentMoment = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentMoment)?.Data;
                documentMoment ??= queueItem.cbReceiptMoment.ToString("yyyy-MM-dd");
                var signatures = new List<SignaturItem>();
                signatures.AddRange(receiptResponse.ftSignatures);
                signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = documentNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = documentMoment,
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment
                        },
                    });
                receiptResponse.ftSignatures = signatures.ToArray();
                break;
            }
            return receiptResponse;
        }
    }
}