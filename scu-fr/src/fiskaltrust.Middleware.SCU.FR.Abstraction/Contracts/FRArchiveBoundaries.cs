using System.Globalization;

namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// What an archive receipt (0x3011) signs about the archive it closes, as v1.2's archive payload
/// (workerFR <c>GetArchivePayload</c>): the queue items and journals that bound the archive, the
/// previous archive receipt, and the day, month and year totals at that moment (market-fr#221,
/// decided 2026-11-09). The receipt fixes which receipts the archive holds, never the archive's bytes:
/// the archive stays verifiable on its own through its own signature (ADR 0007). The previous archive
/// receipt's signature is the chain A link every SCU already signs (ADR 0001).
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>. The queue hands it over in <c>ftStateData.FR.Archive</c>.</remarks>
public class FRArchiveBoundaries
{
    /// <summary>The previous archive receipt's queue item; null for the queue's first archive receipt.</summary>
    public Guid? PreviousArchiveQueueItemId { get; set; }

    public Guid? FirstContainedReceiptQueueItemId { get; set; }

    public DateTime? FirstContainedReceiptMoment { get; set; }

    public Guid? LastContainedReceiptQueueItemId { get; set; }

    public DateTime? LastContainedReceiptMoment { get; set; }

    public Guid? LastReceiptJournalId { get; set; }

    public Guid? LastActionJournalId { get; set; }

    /// <summary>
    /// The last ftJournalFR row, as v1.2 signed it. The v2 queue writes no ftJournalFR and always leaves it null, a
    /// half-migrated queue included (archive-v2 §1.4: <c>LastJournalFRID</c> stays empty); the field keeps its place
    /// in the signed boundaries.
    /// </summary>
    public Guid? LastJournalFRId { get; set; }

    /// <summary>The day totals before the archive receipt closes the day.</summary>
    public FRTotals Day { get; set; } = new();

    public FRTotals Month { get; set; } = new();

    public FRTotals Year { get; set; } = new();

    /// <summary>
    /// The boundaries as one field of a delimited data set (LNE, self-certified and in-memory SCUs):
    /// comma-separated, GUIDs as "D", moments as round-trip UTC, amounts with two decimals; empty when
    /// the receipt is not an archive receipt, so the data set keeps a fixed shape.
    /// </summary>
    public static string SignedField(FRArchiveBoundaries? archive)
        => archive is null ? "" : string.Join(',', new[]
        {
            Id(archive.PreviousArchiveQueueItemId),
            Id(archive.FirstContainedReceiptQueueItemId),
            Moment(archive.FirstContainedReceiptMoment),
            Id(archive.LastContainedReceiptQueueItemId),
            Moment(archive.LastContainedReceiptMoment),
            Id(archive.LastReceiptJournalId),
            Id(archive.LastActionJournalId),
            Id(archive.LastJournalFRId),
        }.Concat(Amounts(archive.Day)).Concat(Amounts(archive.Month)).Concat(Amounts(archive.Year)));

    private static string Id(Guid? id) => id?.ToString("D") ?? "";

    private static string Moment(DateTime? moment) => moment?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "";

    private static IEnumerable<string> Amounts(FRTotals totals) => new[]
    {
        totals.Totalizer, totals.CINormal, totals.CIReduced1, totals.CIReduced2, totals.CIReducedS, totals.CIZero, totals.CIUnknown,
        totals.PICash, totals.PINonCash, totals.PIInternal, totals.PIUnknown,
    }.Select(x => x.ToString("0.00", CultureInfo.InvariantCulture));
}
