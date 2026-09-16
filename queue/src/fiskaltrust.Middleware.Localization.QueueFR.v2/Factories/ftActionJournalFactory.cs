using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;

public static class ftActionJournalFactory
{
    public static ftActionJournal CreateDailyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
        => CreateActionJournal(receiptResponse.ftQueueID, request.ftReceiptCase.ToString("X"), receiptResponse.ftQueueItemID, "Daily-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateMonthlyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
        => CreateActionJournal(receiptResponse.ftQueueID, request.ftReceiptCase.ToString("X"), receiptResponse.ftQueueItemID, "Monthly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateYearlyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
        => CreateActionJournal(receiptResponse.ftQueueID, request.ftReceiptCase.ToString("X"), receiptResponse.ftQueueItemID, "Yearly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateInitialOperationActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new ActivateQueueFR
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = receiptResponse.ftQueueID,
            Moment = DateTime.UtcNow,
            IsStartReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueFR)}", receiptResponse.ftQueueItemID, $"Initial-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
    }

    public static ftActionJournal CreateOutOfOperationActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new DeactivateQueueFR
        {
            CashBoxId = request.ftCashBoxID!.Value,
            QueueId = receiptResponse.ftQueueID,
            Moment = DateTime.UtcNow,
            IsStopReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueFR)}", receiptResponse.ftQueueItemID, $"Out-of-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
    }

    private static ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1) => new()
    {
        ftActionJournalId = Guid.NewGuid(),
        ftQueueId = queueId,
        ftQueueItemId = queueItemId,
        Type = type,
        Moment = DateTime.UtcNow,
        Message = message,
        Priority = priority,
        DataJson = data,
    };
}
