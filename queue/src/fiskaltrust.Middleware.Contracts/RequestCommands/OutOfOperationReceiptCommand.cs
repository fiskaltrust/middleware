using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class OutOfOperationReceiptCommand : RequestCommand
    {
        public OutOfOperationReceiptCommand() { }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem);

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
    }
}
