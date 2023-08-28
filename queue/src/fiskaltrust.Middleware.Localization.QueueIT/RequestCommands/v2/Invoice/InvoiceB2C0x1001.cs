using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Invoice
{
    public class InvoiceB2C0x1001 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;

        public ITReceiptCases ReceiptCase => ITReceiptCases.InvoiceB2C0x1001;

        public bool FailureModeAllowed => true;

        public bool GenerateJournalIT => true;

        public InvoiceB2C0x1001(IITSSCDProvider itSSCDProvider)
        {
            _itSSCDProvider = itSSCDProvider;
        }

        public async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse,
            });
            return new RequestCommandResponse
            {
                ReceiptResponse = result.ReceiptResponse,
                ActionJournals = new List<ftActionJournal>()
            };
        }
    }
}
