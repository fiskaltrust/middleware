using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using System;
using fiskaltrust.Middleware.Contracts.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class OutOfOperationReceipt0x4002 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;

        public ITReceiptCases ReceiptCase => ITReceiptCases.OutOfOperationReceipt0x4002;

        public bool FailureModeAllowed => true;

        public bool GenerateJournalIT => true;

        public OutOfOperationReceipt0x4002(IITSSCDProvider itSSCDProvider)
        {
            _itSSCDProvider = itSSCDProvider;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            if (queue.IsDeactivated())
            {
                return (receiptResponse, new List<ftActionJournal> { ActionJournalFactory.CreateAlreadyOutOfOperationActionJournal(queue, queueItem, request) });
            }

            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse,
            });

            var signatureItem = SignaturItemFactory.CreateOutOfOperationSignature(queueIt);
            var actionJournal = ActionJournalFactory.CreateOutOfOperationActionJournal(queue, queueItem, queueIt, request);
            result.ReceiptResponse.ftSignatures = new SignaturItem[] { signatureItem };
            queue.StopMoment = DateTime.UtcNow;
            return (result.ReceiptResponse, new List<ftActionJournal> { actionJournal });
        }  
    }
}
