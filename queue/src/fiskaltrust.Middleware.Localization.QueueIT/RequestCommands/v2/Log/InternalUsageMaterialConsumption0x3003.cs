using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Log
{
    public class InternalUsageMaterialConsumption0x3003 : IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase => ITReceiptCases.InternalUsageMaterialConsumption0x3003;

        public bool FailureModeAllowed => false;

        public bool GenerateJournalIT => false;

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem) => await Task.FromResult((receiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}
