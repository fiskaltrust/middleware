using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public class ClosingReceiptCommand : RequestCommand
    {
        public ClosingReceiptCommand(IServiceProvider services)
        {
        }

        protected override void AfterCountrySpecificExecuteAsync(ReceiptRequest request, List<ftActionJournal> actionJournals, ReceiptResponse receiptResponse, List<SignaturItem> signatures, Func<ReceiptRequest, ActionJournalEntry> actionJournalEntry) => throw new NotImplementedException();
        protected override List<ftActionJournal> BeforeCountrySpecificExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
        protected override Task<RequestCommandResponse> ExecuteSpecificAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
        protected override ActionJournalEntry GetActionJournalEntry(ReceiptRequest request) => throw new NotImplementedException();
    }
}
