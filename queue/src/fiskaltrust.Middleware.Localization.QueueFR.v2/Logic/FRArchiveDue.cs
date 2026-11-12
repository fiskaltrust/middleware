using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// What a signed entry does to the open archive file (market-fr ADR 0018): at the one-year limit, a
/// <c>ForcedArchive</c> action journal entry naming the file's first and last record and a pending
/// JET 30 for the POS to send; on the response, <see cref="StateFlagsFR.ArchiveIsDue"/> while the file
/// reaches into an earlier month.
/// </summary>
public static class FRArchiveDue
{
    public const int YearlyArchive = 30;
    public const string YearlyArchiveLabel = "Archivage fiscal d'année ou d'exercice";

    /// <summary>Adds the signed entry to the open file and returns the action journal entries to write.</summary>
    public static List<ftActionJournal> Apply(ReceiptRequest request, ReceiptResponse response, FROpenArchiveFile openArchiveFile, FRPendingMessagesState pendingMessages)
    {
        var journals = new List<ftActionJournal>();
        foreach (var file in openArchiveFile.Add(request, response, CertificateSerialNumberOf(response)))
        {
            var forced = ftActionJournalFactory.CreateForcedArchiveActionJournal(request, response, file);
            journals.Add(forced);
            journals.Add(PendingJet30(pendingMessages, response, file, forced.ftActionJournalId));
        }

        if (journals.Count > 0)
        {
            response.ftState = response.ftState.WithFlag(StateFlags.MessageIsPending);
        }

        if (openArchiveFile.IsDue(response.ftReceiptMoment))
        {
            response.ftState = response.ftState.WithFlag(StateFlagsFR.ArchiveIsDue);
        }

        return journals;
    }

    /// <summary>
    /// Raises the pending JET 30 of a forced file. The entry carries the queue item that reached the
    /// limit, so a later roll-forward finds it next to the <c>ForcedArchive</c> entry.
    /// </summary>
    public static ftActionJournal PendingJet30(FRPendingMessagesState pendingMessages, ReceiptResponse response, FROpenArchiveFile.ForcedFile file, Guid forcedArchiveJournalId)
    {
        var journal = pendingMessages.Raise(response.ftQueueID, YearlyArchive, YearlyArchiveLabel, FRJetItems.Information(
            ("closedBy", "yearLimit"),
            ("first.ftQueueItemId", file.First.ftQueueItemId.ToString()),
            ("first.ftReceiptMoment", FRJetItems.Moment(file.First.ftReceiptMoment)),
            ("last.ftQueueItemId", file.Last.ftQueueItemId.ToString()),
            ("last.ftReceiptMoment", FRJetItems.Moment(file.Last.ftReceiptMoment)),
            ("ftActionJournalId", forcedArchiveJournalId.ToString())), response.ftReceiptMoment);
        journal.ftQueueItemId = response.ftQueueItemID;
        return journal;
    }

    /// <summary>Whether <paramref name="journal"/> is the <c>ForcedArchive</c> entry of <paramref name="file"/>.</summary>
    public static bool IsForcedArchiveOf(ftActionJournal journal, FROpenArchiveFile.ForcedFile file)
    {
        if (journal.Type?.EndsWith("-ForcedArchive", StringComparison.Ordinal) != true || string.IsNullOrEmpty(journal.DataJson))
        {
            return false;
        }

        try
        {
            using var data = System.Text.Json.JsonDocument.Parse(journal.DataJson);
            return data.RootElement.GetProperty("First").GetProperty("ftQueueItemId").GetGuid() == file.First.ftQueueItemId
                && data.RootElement.GetProperty("Last").GetProperty("ftQueueItemId").GetGuid() == file.Last.ftQueueItemId;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Whether <paramref name="journal"/> is the pending JET 30 raised for <paramref name="file"/>.</summary>
    public static bool IsPendingJet30Of(ftActionJournal journal, FROpenArchiveFile.ForcedFile file)
        => journal.Type == $"{YearlyArchive}{FRPendingMessagesState.JournalTypeSuffix}"
            && journal.DataJson?.Contains(file.First.ftQueueItemId.ToString(), StringComparison.Ordinal) == true
            && journal.DataJson.Contains(file.Last.ftQueueItemId.ToString(), StringComparison.Ordinal);

    /// <summary>The certificate serial the entry was signed under, read from its receipt QR code.</summary>
    public static string? CertificateSerialNumberOf(ReceiptResponse response) => FRJetWatch.Installation.Of(null, response).CertificateSerialNumber;
}
