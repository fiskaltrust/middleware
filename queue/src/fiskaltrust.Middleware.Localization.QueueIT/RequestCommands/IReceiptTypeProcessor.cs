using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public interface IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase { get; }

        public bool FailureModeAllowed { get; }

        public bool GenerateJournalIT { get; }

        Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem);
    }
}
