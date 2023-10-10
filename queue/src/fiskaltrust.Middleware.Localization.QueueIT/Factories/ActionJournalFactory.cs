using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories
{
    public static class ActionJournalFactory

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

        public static ftActionJournal CreateInitialOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ftQueueIT queueIT, ReceiptRequest request)
        {
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0",
            };
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}", queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
        }

        public static ftActionJournal CreateWrongStateForInitialOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}",
                    queueItem.ftQueueItemId, queue.IsDeactivated()
                            ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                            : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");
        }

        public static ftActionJournal CreateOutOfOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ftQueueIT queueIT, ReceiptRequest request)
        {
            var notification = new DeactivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStopReceipt = true,
                Version = "V0"
            };
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueSCU)}", queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
        }

        public static ftActionJournal CreateAlreadyOutOfOperationActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-Queue-already-deactivated",
                queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", $"Queue was already deactivated on the {queue.StopMoment.Value.ToString("yyyy-MM-dd hh:mm:ss")}");
        }

        public static ftActionJournal CreateYearlyClosingClosingActionJournal(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request)
        {
            var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
            return CreateActionJournal(queue.ftQueueId, ftReceiptCaseHex, queueItem.ftQueueItemId, $"Yearly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
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
