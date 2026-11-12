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
            journals.Add(pendingMessages.Raise(response.ftQueueID, YearlyArchive, YearlyArchiveLabel, FRJetItems.Information(
                ("closedBy", "yearLimit"),
                ("first.ftQueueItemId", file.First.ftQueueItemId.ToString()),
                ("first.ftReceiptMoment", FRJetItems.Moment(file.First.ftReceiptMoment)),
                ("last.ftQueueItemId", file.Last.ftQueueItemId.ToString()),
                ("last.ftReceiptMoment", FRJetItems.Moment(file.Last.ftReceiptMoment)),
                ("ftActionJournalId", forced.ftActionJournalId.ToString())), response.ftReceiptMoment));
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

    /// <summary>The certificate serial the entry was signed under, read from its receipt QR code.</summary>
    public static string? CertificateSerialNumberOf(ReceiptResponse response) => FRJetWatch.Installation.Of(null, response).CertificateSerialNumber;
}
