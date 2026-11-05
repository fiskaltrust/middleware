using fiskaltrust.ifPOS.v2;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.Factories;

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

    public static ftActionJournal CreateYearlyClosingActionJournal(ftQueue queue, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
        return CreateActionJournal(receiptResponse.ftQueueID, ftReceiptCaseHex, receiptResponse.ftQueueItemID, $"Yearly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));
    }

    /// <summary>
    /// The activation notification. It keeps the <see cref="ActivateQueueSCU"/> payload and the
    /// <c>-ActivateQueueSCU</c> type suffix the Italian queue has always written, so the consumers of the
    /// action journal see the same entry as before the move to the v2 stream.
    /// </summary>
    public static ftActionJournal CreateInitialOperationActionJournal(ftQueueIT queueIT, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new ActivateQueueSCU
        {
            CashBoxId = request.ftCashBoxID.GetValueOrDefault(),
            QueueId = receiptResponse.ftQueueID,
            Moment = DateTime.UtcNow,
            SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
            IsStartReceipt = true,
            Version = "V0",
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}", receiptResponse.ftQueueItemID, $"Initial-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
    }

    public static ftActionJournal CreateOutOfOperationActionJournal(ftQueueIT queueIT, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var notification = new DeactivateQueueSCU
        {
            CashBoxId = request.ftCashBoxID.GetValueOrDefault(),
            QueueId = receiptResponse.ftQueueID,
            Moment = DateTime.UtcNow,
            SCUId = queueIT.ftSignaturCreationUnitITId.GetValueOrDefault(),
            IsStopReceipt = true,
            Version = "V0"
        };
        return CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-{nameof(DeactivateQueueSCU)}", receiptResponse.ftQueueItemID, $"Out-of-Operation receipt. Queue-ID: {receiptResponse.ftQueueID}", JsonConvert.SerializeObject(notification));
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
