using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class RequestCommand
    {
        public async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            var actionJournals = BeforeCountrySpecificExecuteAsync(queue, request, queueItem);

            var response = await ExecuteSpecificAsync(queue, queueDE, request, queueItem).ConfigureAwait(false);

            response.ActionJournals.AddRange(actionJournals);

            var func = new Func<ReceiptRequest, ActionJournalEntry>(GetActionJournalEntry);

            AfterCountrySpecificExecuteAsync(request, response.ActionJournals, response.ReceiptResponse, response.Signatures, func);

            return response;
        }

        protected abstract ActionJournalEntry GetActionJournalEntry(ReceiptRequest request);
        protected abstract Task<RequestCommandResponse> ExecuteSpecificAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem);
        protected abstract List<ftActionJournal> BeforeCountrySpecificExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);
        protected abstract void AfterCountrySpecificExecuteAsync(ReceiptRequest request, List<ftActionJournal> actionJournals, ReceiptResponse receiptResponse, List<SignaturItem> signatures, Func<ReceiptRequest, ActionJournalEntry> actionJournalEntry);

    }
}