using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Log
{
    public class Order0x3004 : IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase => ITReceiptCases.Order0x3004;

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem) => await Task.FromResult((receiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}
