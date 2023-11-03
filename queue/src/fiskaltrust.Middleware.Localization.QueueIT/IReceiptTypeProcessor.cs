using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public interface IReceiptTypeProcessor
    {
        public ReceiptCases ReceiptCase { get; }

        Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem);
    }
}
