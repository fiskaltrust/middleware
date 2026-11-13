using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The archive file a French queue keeps open (market-fr ADR 0018): the signed entries not yet closed
/// by an archive receipt, a forced archive or a change of signing certificate. The queue flags an
/// archive as due once the file reaches into an earlier month, and at the one-year limit it records a
/// forced archive - it never signs anything on its own. The state lives in ftQueueFR
/// (<see cref="FRStateV2"/>, ADR 0012), written with the rest of the chain state.
/// </summary>
public sealed class FROpenArchiveFile
{
    /// <summary>A signed entry of the file: its queue item and the queue's moment (ftReceiptMoment, UTC).</summary>
    public sealed record Record(Guid ftQueueItemId, DateTime ftReceiptMoment);

    /// <summary>A file cut at the one-year limit, without an archive receipt.</summary>
    public sealed record ForcedFile(Record First, Record Last);

    private readonly ftQueueFR _queueFR;

    public FROpenArchiveFile(ftQueueFR queueFR)
    {
        _queueFR = queueFR;
    }

    /// <summary>
    /// The limit of a file whose first record is <paramref name="firstMoment"/>: an entry stays in it while its moment is
    /// earlier (v1.2 <c>workerFR.cs:756</c>). The archive export cuts with the same expression, so the queue and the
    /// export cannot disagree on where a file ends.
    /// </summary>
    public static DateTime LimitOf(DateTime firstMoment) => fiskaltrust.Middleware.Journal.FR.Archive.Split.ArchiveSplitter.YearLimit(firstMoment);

    /// <summary>
    /// Adds a signed entry and returns the files the one-year limit cut before it, oldest first. A file
    /// cut at the limit ends on the last daily closing signed in it, and the entries after that closing
    /// open the next file; with no daily closing it ends on the entry before the new one. A change of
    /// signing certificate opens a new file, and a non-training archive receipt closes the file, itself
    /// included.
    /// </summary>
    public List<ForcedFile> Add(ReceiptRequest request, ReceiptResponse response, string? certificateSerialNumber)
    {
        var state = Read();
        var entry = new Record(response.ftQueueItemID, response.ftReceiptMoment.ToUniversalTime());
        var forced = new List<ForcedFile>();

        if (state.First is not null && certificateSerialNumber is not null && state.CertificateSerialNumber is not null && certificateSerialNumber != state.CertificateSerialNumber)
        {
            state.Restart();
        }

        while (state.First is not null && entry.ftReceiptMoment >= LimitOf(state.First.ftReceiptMoment))
        {
            if (state.LastDailyClosing is not null)
            {
                forced.Add(new ForcedFile(state.First, state.LastDailyClosing));
                state.First = state.AfterLastDailyClosing;
                state.LastDailyClosing = null;
                state.AfterLastDailyClosing = null;
                if (state.First is null)
                {
                    state.Last = null;
                }
            }
            else
            {
                forced.Add(new ForcedFile(state.First, state.Last!));
                state.Restart();
            }
        }

        var training = request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training);
        state.First ??= entry;
        if (state.LastDailyClosing is not null && state.AfterLastDailyClosing is null)
        {
            state.AfterLastDailyClosing = entry;
        }
        if (!training && request.ftReceiptCase.Case() == ReceiptCase.DailyClosing0x2011)
        {
            state.LastDailyClosing = entry;
            state.AfterLastDailyClosing = null;
        }
        state.Last = entry;
        state.CertificateSerialNumber = certificateSerialNumber ?? state.CertificateSerialNumber;

        if (!training && request.ftReceiptCase.Case() == FRReceiptChainExt.ArchiveReceipt)
        {
            state.Restart();
        }

        Write(state);
        return forced;
    }

    /// <summary>True when the open file's first record lies in an earlier calendar month (UTC) than <paramref name="moment"/>.</summary>
    public bool IsDue(DateTime moment)
    {
        var first = Read().First;
        if (first is null)
        {
            return false;
        }

        var utc = moment.ToUniversalTime();
        return first.ftReceiptMoment.Year * 12 + first.ftReceiptMoment.Month < utc.Year * 12 + utc.Month;
    }

    private FileState Read() => FRStateV2.Read(_queueFR).OpenArchiveFile ?? new FileState();

    private void Write(FileState state) => FRStateV2.Update(_queueFR, x => x.OpenArchiveFile = state);

    /// <summary>The stored form of the open file.</summary>
    public sealed class FileState
    {
        public Record? First { get; set; }

        public Record? Last { get; set; }

        public Record? LastDailyClosing { get; set; }

        public Record? AfterLastDailyClosing { get; set; }

        public string? CertificateSerialNumber { get; set; }

        public void Restart()
        {
            First = null;
            Last = null;
            LastDailyClosing = null;
            AfterLastDailyClosing = null;
        }
    }
}
