﻿using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueGR.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR.Factories;

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
        var notification = new ActivateQueueGR
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = queueItem.ftQueueId,
            Moment = DateTime.UtcNow,
            IsStartReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueGR)}", queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
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
        var notification = new DeactivateQueueGR
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = queueItem.ftQueueId,
            Moment = DateTime.UtcNow,
            IsStopReceipt = true,
            Version = "V0"
        };
        return CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueGR)}", queueItem.ftQueueItemId, $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));
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
