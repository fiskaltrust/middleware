using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;

public static class ftActionJournalFactory
{
    /// <summary>A signed closing: dated by its receipt moment, with its identification, the period it closed and its totals.</summary>
    public static ftActionJournal CreateClosingActionJournal(ReceiptRequest request, ReceiptResponse signedResponse, string message)
    {
        var fr = MiddlewareStateData.FromReceiptResponse(signedResponse).FR;
        var journal = CreateActionJournal(signedResponse.ftQueueID, request.ftReceiptCase.ToString("X"), signedResponse.ftQueueItemID, message, JsonConvert.SerializeObject(new
        {
            signedResponse.ftReceiptIdentification,
            signedResponse.ftReceiptMoment,
            fr?.Closing?.ClosingType,
            fr?.Closing?.PeriodStart,
            fr?.Closing?.PeriodEnd,
            fr?.Totals,
        }));
        journal.Moment = signedResponse.ftReceiptMoment;
        return journal;
    }

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

    /// <summary>
    /// A file cut at the one-year limit without an archive receipt (market-fr ADR 0018). The portal
    /// builds that fixed file from the first and last record named here.
    /// </summary>
    public static ftActionJournal CreateForcedArchiveActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse, Logic.FROpenArchiveFile.ForcedFile file)
        => CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-ForcedArchive", receiptResponse.ftQueueItemID,
            $"The archive file from queue item {file.First.ftQueueItemId} to {file.Last.ftQueueItemId} reached the one-year limit: it is archived without an archive receipt.",
            JsonConvert.SerializeObject(new
            {
                First = new { file.First.ftQueueItemId, ftReceiptMoment = file.First.ftReceiptMoment.ToUniversalTime().ToString("O") },
                Last = new { file.Last.ftQueueItemId, ftReceiptMoment = file.Last.ftReceiptMoment.ToUniversalTime().ToString("O") },
            }));

    public static ftActionJournal CreateLateSigningStartedActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse)
        => CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-LateSigningStarted", receiptResponse.ftQueueItemID,
            $"QueueItem {receiptResponse.ftQueueItemID} ({receiptResponse.ftReceiptIdentification}) opened the late-signing mode.",
            JsonConvert.SerializeObject(new { request.cbReceiptReference, receiptResponse.ftReceiptIdentification, request.cbReceiptMoment }));

    public static ftActionJournal CreateLateSigningEndedActionJournal(ReceiptRequest request, ReceiptResponse receiptResponse, Logic.FRLateSigningRange range, string? firstReceiptReference, string? firstReceiptIdentification)
        => CreateActionJournal(receiptResponse.ftQueueID, $"{request.ftReceiptCase:X}-LateSigningEnded", receiptResponse.ftQueueItemID,
            $"Failure registered: {range.Count} late-signed receipt(s) from {firstReceiptIdentification} to {receiptResponse.ftReceiptIdentification}.",
            JsonConvert.SerializeObject(new
            {
                range.Count,
                range.FirstQueueItemId,
                FirstReceiptReference = firstReceiptReference,
                FirstReceiptIdentification = firstReceiptIdentification,
                cbReceiptMomentMin = range.MomentMin,
                cbReceiptMomentMax = range.MomentMax,
                ClosingReceiptReference = request.cbReceiptReference,
                ClosingReceiptIdentification = receiptResponse.ftReceiptIdentification,
            }));

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
