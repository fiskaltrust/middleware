using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class ClosingReceiptCommand : RequestCommand
    {
        protected abstract string ClosingReceiptName { get; }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isResend = false)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, await GetCashboxIdentificationAsync(queue.ftQueueId), CountryBaseState);
            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}", queueItem.ftQueueItemId, $"{ClosingReceiptName} receipt was processed.",
                JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
            var requestCommandResponse = new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournalEntry }
            };
            return await SpecializeAsync(requestCommandResponse, queue, request, queueItem);
        }

        // This is overkill for now ... just to demonstrate how we could maybe add some country specific funtionality
        protected virtual Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(requestCommandResponse);
        
        protected abstract Task<string> GetCashboxIdentificationAsync(Guid ftQueueId);
    }
}
