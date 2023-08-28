using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using fiskaltrust.storage.serialization.DE.V0;
using Newtonsoft.Json;
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
                var actionjournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-Queue-already-deactivated",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", $"Queue was already deactivated on the {queue.StopMoment.Value.ToString("yyyy-MM-dd hh:mm:ss")}");
                return (receiptResponse, new List<ftActionJournal> { actionjournal });
            }
            var (actionJournal, signatureItem) = await DeactivateSCUAsync(queue, queueIt, request, queueItem);
            receiptResponse.ftSignatures = new SignaturItem[] { signatureItem };
            queue.StopMoment = DateTime.UtcNow;
            return (receiptResponse, new List<ftActionJournal> { actionJournal });
        }

        protected Task<(ftActionJournal, SignaturItem)> DeactivateSCUAsync(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var signatureItem = CreateOutOfOperationSignature($"Queue-ID: {queue.ftQueueId}");
            var notification = new DeactivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStopReceipt = true,
                Version = "V0"
            };

            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return Task.FromResult((actionJournal, signatureItem));
        }

        public SignaturItem CreateOutOfOperationSignature(string data)
        {
            return new SignaturItem()
            {
                ftSignatureType = Cases.BASE_STATE & 0x4,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = data
            };
        }

        protected ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftQueueItemId = queueItemId,
                Type = type,
                Moment = DateTime.UtcNow,
                Message = message,
                Priority = priority,
                DataJson = data
            };
        }
    }
}
