using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories
{
    public static class ftActionJournalFactory
    {
        public static ftActionJournal CreateDailyClosingActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
            return CreateActionJournal(queue.ftQueueId, ftReceiptCaseHex, queueItem.ftQueueItemId, $"Daily-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
        }

        public static ftActionJournal CreateMonthlyClosingActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
            return CreateActionJournal(queue.ftQueueId, ftReceiptCaseHex, queueItem.ftQueueItemId, $"Monthly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
        }

        public static ftActionJournal CreateInitialOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var notification = new ActivateQueuePT
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                IsStartReceipt = true,
                Version = "V0",
            };
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-ActivateQueuePT", queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
        }

        public static ftActionJournal CreateWrongStateForInitialOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                    queueItem.ftQueueItemId, queue.IsDeactivated()
                            ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                            : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");
        }

        public static ftActionJournal CreateOutOfOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var notification = new DeactivateQueuePT
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                IsStopReceipt = true,
                Version = "V0"
            };
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueuePT)}", queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
        }

        private static ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
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
