using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class FinishSCUSwitch0x4012 : IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase => ITReceiptCases.FinishSCUSwitch0x4012;

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem) => await Task.FromResult((receiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}
