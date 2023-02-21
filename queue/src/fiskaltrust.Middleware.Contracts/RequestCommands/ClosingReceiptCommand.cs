using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public abstract class ClosingReceiptCommand : RequestCommand
    {
        protected abstract string ClosingReceiptName { get; }
        public Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, CountryBaseState);
            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}", queueItem.ftQueueItemId, $"{ClosingReceiptName} receipt was processed.",
                JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
            var requestCommandResponse = new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal> { actionJournalEntry }
            };
            return SpecializeAsync(requestCommandResponse);
        }

        // This is overkill for now ... just to demonstrate how we could maybe add some country specific funtionality
        protected virtual Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse) => Task.FromResult(requestCommandResponse);
    }
}
