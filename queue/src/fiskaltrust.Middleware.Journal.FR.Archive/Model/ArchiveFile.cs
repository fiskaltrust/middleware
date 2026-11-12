using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Model
{
    /// <summary>Why an archive file ends (contract section 10.2, <c>closedBy</c>).</summary>
    public enum ArchiveClosedBy
    {
        /// <summary>The last record of the requested period.</summary>
        PeriodEnd,

        /// <summary>The next record is signed with another certificate.</summary>
        LastSignedEntry,

        /// <summary>The next record has another format version: the half migration.</summary>
        FormatChange,

        /// <summary>The next record is at or past the one-year limit of the file's first record (ADR 0018).</summary>
        YearLimit,

        /// <summary>An archive receipt, included (a local export, later).</summary>
        ArchiveReceipt,
    }

    public static class ArchiveClosedByExtensions
    {
        /// <summary>The manifest value: <c>periodEnd</c>, <c>lastSignedEntry</c>, <c>formatChange</c>, <c>yearLimit</c> or <c>archiveReceipt</c>.</summary>
        public static string ToManifestValue(this ArchiveClosedBy closedBy) => closedBy switch
        {
            ArchiveClosedBy.PeriodEnd => "periodEnd",
            ArchiveClosedBy.LastSignedEntry => "lastSignedEntry",
            ArchiveClosedBy.FormatChange => "formatChange",
            ArchiveClosedBy.YearLimit => "yearLimit",
            ArchiveClosedBy.ArchiveReceipt => "archiveReceipt",
            _ => throw new ArgumentOutOfRangeException(nameof(closedBy)),
        };
    }

    /// <summary>
    /// One archive file (ADR 0019): a standalone signed zip holding the entries of one SCU certificate and one format
    /// version, in queue order, never more than a year. There is no main archive and no sub-archive. A requested file
    /// states its slice and its place in the request; a fixed file (ADR 0018) is bounded by its records only.
    /// </summary>
    public sealed class ArchiveFile<TRecord>
    {
        /// <summary>A requested file: one part of a requested period, with its own slice.</summary>
        public ArchiveFile(ArchiveFileKey key, IReadOnlyList<TRecord> records, DateTime firstRecordMoment, DateTime lastRecordMoment,
            ArchivePeriod period, ArchivePeriod requestedPeriod, int part, int partCount, ArchiveClosedBy closedBy)
            : this(key, records, firstRecordMoment, lastRecordMoment, closedBy)
        {
            if (part < 1 || part > partCount)
            {
                throw new ArgumentOutOfRangeException(nameof(part), $"Part {part} of {partCount}: parts are numbered from 1.");
            }
            Period = period ?? throw new ArgumentNullException(nameof(period));
            RequestedPeriod = requestedPeriod ?? throw new ArgumentNullException(nameof(requestedPeriod));
            Part = part;
            PartCount = partCount;
        }

        /// <summary>A fixed file: bounded by its first and last record, without slice or request (ADR 0018, contract section 10.3).</summary>
        public ArchiveFile(ArchiveFileKey key, IReadOnlyList<TRecord> records, DateTime firstRecordMoment, DateTime lastRecordMoment, ArchiveClosedBy closedBy, string? forcedArchive)
            : this(key, records, firstRecordMoment, lastRecordMoment, closedBy)
        {
            ForcedArchive = forcedArchive;
        }

        private ArchiveFile(ArchiveFileKey key, IReadOnlyList<TRecord> records, DateTime firstRecordMoment, DateTime lastRecordMoment, ArchiveClosedBy closedBy)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Records = records ?? throw new ArgumentNullException(nameof(records));
            if (records.Count == 0)
            {
                throw new ArgumentException("An archive file holds at least one record: a range without records gives no file.", nameof(records));
            }
            FirstRecordMoment = Utc.Normalize(firstRecordMoment);
            LastRecordMoment = Utc.Normalize(lastRecordMoment);
            ClosedBy = closedBy;
        }

        public ArchiveFileKey Key { get; }

        /// <summary>The file's records, in queue order.</summary>
        public IReadOnlyList<TRecord> Records { get; }

        public TRecord FirstRecord => Records[0];

        public TRecord LastRecord => Records[Records.Count - 1];

        public DateTime FirstRecordMoment { get; }

        public DateTime LastRecordMoment { get; }

        /// <summary>The file's slice <c>[periodStart, periodEnd)</c>, never more than a year (contract section 10.3); null on a fixed file.</summary>
        public ArchivePeriod? Period { get; }

        /// <summary>The whole period that was asked for, which the file may be one part of; null on a fixed file.</summary>
        public ArchivePeriod? RequestedPeriod { get; }

        /// <summary>The file's place among the files the request was split into, from 1; null on a fixed file.</summary>
        public int? Part { get; }

        public int? PartCount { get; }

        /// <summary>On a fixed file at the one-year limit: the <c>ForcedArchive</c> action journal entry's id (ADR 0018).</summary>
        public string? ForcedArchive { get; }

        public ArchiveClosedBy ClosedBy { get; }
    }
}
