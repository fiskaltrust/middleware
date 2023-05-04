using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;
using fiskaltrust.Middleware.Contracts.Extensions;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class OutOfOperationReceiptCommand : RequestCommand
    {
        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isResend = false)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, await GetCashboxIdentificationAsync(queue.ftQueueId));

            if (queue.IsDeactivated())
            {
                var actionjournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-Queue-already-deactivated",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", $"Queue was alreády deactivated on the {queue.StopMoment.Value.ToString("yyyy-MM-dd hh:mm:ss")}");
                return new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal> { actionjournal }
                };
            }
            var (actionJournal, signatureItem) = await DeactivateSCUAsync(queue, request, queueItem);
            receiptResponse.ftSignatures = new SignaturItem[] { signatureItem };

            queue.StopMoment = DateTime.UtcNow;

            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournal }
            };
        }

        protected abstract Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected abstract Task<string> GetCashboxIdentificationAsync(Guid ftQueueId);
    }
}
