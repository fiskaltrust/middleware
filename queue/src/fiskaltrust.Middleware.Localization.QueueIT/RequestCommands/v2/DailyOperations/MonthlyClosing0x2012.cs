using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.DailyOperations
{
    public class MonthlyClosing0x2012 : IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase => ITReceiptCases.MonthlyClosing0x2012;

        public bool FailureModeAllowed => false;

        public bool GenerateJournalIT => false;

        public async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            return await Task.FromResult(new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
            }).ConfigureAwait(false);
        }
    }
}
