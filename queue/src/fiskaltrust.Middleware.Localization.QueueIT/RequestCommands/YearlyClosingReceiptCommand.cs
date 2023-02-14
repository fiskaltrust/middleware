using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    internal class YearlyClosingReceiptCommand : ClosingReceiptCommand
    {
        public YearlyClosingReceiptCommand(IServiceProvider services) : base(services) { }

        protected override Task<RequestCommandResponse> ExecuteSpecificAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
        protected override ActionJournalEntry GetActionJournalEntry(ReceiptRequest request) => throw new NotImplementedException();

    }
}
