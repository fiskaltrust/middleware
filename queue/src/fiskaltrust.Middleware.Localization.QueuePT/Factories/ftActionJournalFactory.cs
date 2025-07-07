using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories;

public static class ftActionJournalFactory
{
    public static ftActionJournal CreateDailyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
        return CreateActionJournal(receiptResponse.ftQueueID, ftReceiptCaseHex, receiptResponse.ftQueueItemID, $"Daily-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
    }

    public static ftActionJournal CreateMonthlyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
        return CreateActionJournal(receiptResponse.ftQueueID, ftReceiptCaseHex, receiptResponse.ftQueueItemID, $"Monthly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
    }

    public static ftActionJournal CreateInitialOperationActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new ActivateQueuePT
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = receiptResponse.ftQueueID,
            Moment = DateTime.UtcNow,
            IsStartReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(ActivateQueuePT)}", receiptResponse.ftQueueItemID, $"Initial-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
    }

    public static ftActionJournal CreateWrongStateForInitialOperationActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}",
                receiptResponse.ftQueueItemID, queue.IsDeactivated()
                        ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                        : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed.", "");
    }

    public static ftActionJournal CreateOutOfOperationActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new DeactivateQueuePT
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = receiptResponse.ftQueueItemID,
            Moment = DateTime.UtcNow,
            IsStopReceipt = true,
            Version = "V0"
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueuePT)}", receiptResponse.ftQueueItemID, $"Out-of-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
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
