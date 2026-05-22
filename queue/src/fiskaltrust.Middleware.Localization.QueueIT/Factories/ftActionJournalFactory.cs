using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

public static class ftActionJournalFactory
{
    public static ftActionJournal CreateDailyClosingActionJournal(ftQueue queue, Guid queueItemId, ReceiptRequest request)
        => CreateActionJournal(queue.ftQueueId, ((long) request.ftReceiptCase).ToString("X"), queueItemId,
            "Daily-Closing receipt was processed.",
            JsonSerializer.Serialize(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateMonthlyClosingActionJournal(ftQueue queue, Guid queueItemId, ReceiptRequest request)
        => CreateActionJournal(queue.ftQueueId, ((long) request.ftReceiptCase).ToString("X"), queueItemId,
            "Monthly-Closing receipt was processed.",
            JsonSerializer.Serialize(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateYearlyClosingClosingActionJournal(ftQueue queue, Guid queueItemId, ReceiptRequest request)
        => CreateActionJournal(queue.ftQueueId, ((long) request.ftReceiptCase).ToString("X"), queueItemId,
            "Yearly-Closing receipt was processed.",
            JsonSerializer.Serialize(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

    public static ftActionJournal CreateInitialOperationActionJournal(ftQueue queue, Guid queueItemId, ftQueueIT queueIT, ReceiptRequest request)
    {
        var payload = new
        {
            CashBoxId = request.ftCashBoxID,
            QueueId = queue.ftQueueId,
            Moment = DateTime.UtcNow,
            SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
            IsStartReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(queue.ftQueueId, $"{(long) request.ftReceiptCase:X}-ActivateQueueSCU", queueItemId,
            $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}",
            JsonSerializer.Serialize(payload));
    }

    public static ftActionJournal CreateOutOfOperationActionJournal(ftQueue queue, Guid queueItemId, ftQueueIT queueIT, ReceiptRequest request)
    {
        var payload = new
        {
            CashBoxId = request.ftCashBoxID,
            QueueId = queue.ftQueueId,
            Moment = DateTime.UtcNow,
            SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
            IsStopReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(queue.ftQueueId, $"{(long) request.ftReceiptCase:X}-DeactivateQueueSCU", queueItemId,
            $"Out-of-Operation receipt. Queue-ID: {queue.ftQueueId}",
            JsonSerializer.Serialize(payload));
    }

    private static ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
        => new()
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
