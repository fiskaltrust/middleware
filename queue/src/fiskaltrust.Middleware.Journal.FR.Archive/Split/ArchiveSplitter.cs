using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Split
{
    /// <summary>
    /// Cuts the records of a requested period into archive files (ADR 0019, contract section 10.3): one file per
    /// SCU certificate and format version, never more than a year, each stating its own slice.
    /// </summary>
    public static class ArchiveSplitter
    {
        /// <summary>
        /// The one-year limit of a file whose first record is at <paramref name="firstMoment"/> (ADR 0018): the file keeps
        /// an entry while <c>entry.ftReceiptMoment &lt; first.ftReceiptMoment.AddYears(1).Date</c>, in UTC.
        /// </summary>
        public static DateTime YearLimit(DateTime firstMoment) => DateTime.SpecifyKind(Utc.Normalize(firstMoment).AddYears(1).Date, DateTimeKind.Utc);

        /// <summary>
        /// The records a request delivers (contract section 10.3): in queue order, from the first whose moment is at or
        /// after <paramref name="requested"/>'s start to the last whose moment is before its end, keeping every record
        /// between them whatever its moment (a clock that jumped), since the chains need it. Empty when none is in the
        /// period.
        /// </summary>
        /// <param name="recordsInQueueOrder">The candidate records, in queue order.</param>
        public static IReadOnlyList<TRecord> Select<TRecord>(IReadOnlyList<TRecord> recordsInQueueOrder, Func<TRecord, DateTime> momentOf, ArchivePeriod requested)
        {
            if (recordsInQueueOrder is null) throw new ArgumentNullException(nameof(recordsInQueueOrder));
            if (momentOf is null) throw new ArgumentNullException(nameof(momentOf));
            if (requested is null) throw new ArgumentNullException(nameof(requested));

            var first = -1;
            for (var i = 0; i < recordsInQueueOrder.Count; i++)
            {
                if (Utc.Normalize(momentOf(recordsInQueueOrder[i])) >= requested.Start)
                {
                    first = i;
                    break;
                }
            }
            var last = -1;
            for (var i = recordsInQueueOrder.Count - 1; i >= 0; i--)
            {
                if (Utc.Normalize(momentOf(recordsInQueueOrder[i])) < requested.End)
                {
                    last = i;
                    break;
                }
            }
            if (first < 0 || last < first)
            {
                return Array.Empty<TRecord>();
            }
            var selected = new List<TRecord>(last - first + 1);
            for (var i = first; i <= last; i++)
            {
                selected.Add(recordsInQueueOrder[i]);
            }
            return selected;
        }

        /// <summary>
        /// The files of a request: its records (<see cref="Select{TRecord}"/>) cut, in queue order, when the next record has
        /// another certificate, another format version (the half migration), or is at or past the year limit of the file's
        /// first record. An archive receipt is no cut point. A request without records gives no file.
        /// </summary>
        /// <param name="recordsInQueueOrder">The candidate records, in queue order; those outside the request are left out as <see cref="Select{TRecord}"/> says.</param>
        public static IReadOnlyList<ArchiveFile<TRecord>> Split<TRecord>(
            IReadOnlyList<TRecord> recordsInQueueOrder,
            Func<TRecord, DateTime> momentOf,
            Func<TRecord, ArchiveFileKey> keyOf,
            ArchivePeriod requested)
        {
            if (keyOf is null) throw new ArgumentNullException(nameof(keyOf));
            var records = Select(recordsInQueueOrder, momentOf, requested);

            var runs = new List<Run<TRecord>>();
            Run<TRecord>? current = null;
            foreach (var record in records)
            {
                var moment = Utc.Normalize(momentOf(record));
                var key = keyOf(record) ?? throw new ArgumentException("Every record needs an archive file key.", nameof(keyOf));
                var closedBy = current is null ? (ArchiveClosedBy?) null
                    : !string.Equals(key.CertificateSerialNumber, current.Key.CertificateSerialNumber, StringComparison.OrdinalIgnoreCase)
                        ? key.FormatVersion.Equals(current.Key.FormatVersion) ? ArchiveClosedBy.LastSignedEntry : ArchiveClosedBy.FormatChange
                    : !key.FormatVersion.Equals(current.Key.FormatVersion) ? ArchiveClosedBy.FormatChange
                    : moment >= current.Limit ? ArchiveClosedBy.YearLimit
                    : (ArchiveClosedBy?) null;
                if (current is null || closedBy is not null)
                {
                    if (current is not null)
                    {
                        current.ClosedBy = closedBy!.Value;
                    }
                    current = new Run<TRecord>(key, moment);
                    runs.Add(current);
                }
                current.Add(record, moment);
            }

            var files = new List<ArchiveFile<TRecord>>(runs.Count);
            DateTime? previousEnd = null;
            for (var i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                var end = Min(requested.End, run.Limit);
                if (i + 1 < runs.Count && runs[i + 1].FirstMoment < end)
                {
                    end = runs[i + 1].FirstMoment;
                }
                if (end <= run.LastMoment)
                {
                    // A clock set back at the cut: the slice still holds its own last record.
                    end = run.LastMoment.AddTicks(1);
                }

                var start = previousEnd ?? requested.Start;
                if (start > run.FirstMoment || end > start.AddYears(1))
                {
                    start = run.FirstMoment;
                }
                files.Add(new ArchiveFile<TRecord>(run.Key, run.Records, run.FirstMoment, run.LastMoment, new ArchivePeriod(start, end), requested,
                    i + 1, runs.Count, i + 1 < runs.Count ? run.ClosedBy : ArchiveClosedBy.PeriodEnd));
                previousEnd = end;
            }
            return files;
        }

        /// <summary>
        /// A fixed file (ADR 0018, contract section 10.3): the records from <paramref name="firstId"/> to <paramref name="lastId"/>
        /// included, in queue order, bounded by records and not by dates, as a <c>ForcedArchive</c> entry names them.
        /// </summary>
        /// <param name="recordsInQueueOrder">Candidate records in queue order; they must hold both named records.</param>
        /// <param name="forcedArchive">The <c>ForcedArchive</c> action journal entry's id, written to the manifest.</param>
        /// <exception cref="ArgumentException">
        /// A named record is missing or the last comes before the first, the range crosses a cut (another certificate or
        /// format version), or a record is at or past the year limit of the first. The closing rule (the file ends on the
        /// last daily closing before the limit) is the caller's check: this package does not know daily closings.
        /// </exception>
        public static ArchiveFile<TRecord> Range<TRecord>(
            IReadOnlyList<TRecord> recordsInQueueOrder,
            Func<TRecord, Guid> idOf,
            Guid firstId,
            Guid lastId,
            Func<TRecord, DateTime> momentOf,
            Func<TRecord, ArchiveFileKey> keyOf,
            ArchiveClosedBy closedBy = ArchiveClosedBy.YearLimit,
            string? forcedArchive = null)
        {
            if (recordsInQueueOrder is null) throw new ArgumentNullException(nameof(recordsInQueueOrder));
            if (idOf is null) throw new ArgumentNullException(nameof(idOf));
            if (momentOf is null) throw new ArgumentNullException(nameof(momentOf));
            if (keyOf is null) throw new ArgumentNullException(nameof(keyOf));

            var first = -1;
            var last = -1;
            for (var i = 0; i < recordsInQueueOrder.Count; i++)
            {
                var id = idOf(recordsInQueueOrder[i]);
                if (first < 0 && id == firstId) first = i;
                if (id == lastId) last = i;
            }
            if (first < 0 || last < 0)
            {
                throw new ArgumentException($"The record range {firstId} to {lastId} names a record that is not among the records given.", nameof(recordsInQueueOrder));
            }
            if (last < first)
            {
                throw new ArgumentException($"The record range ends ({lastId}) before it starts ({firstId}) in queue order.", nameof(lastId));
            }

            var key = keyOf(recordsInQueueOrder[first]);
            var firstMoment = Utc.Normalize(momentOf(recordsInQueueOrder[first]));
            var limit = YearLimit(firstMoment);
            var records = new List<TRecord>(last - first + 1);
            for (var i = first; i <= last; i++)
            {
                var record = recordsInQueueOrder[i];
                if (!key.Equals(keyOf(record)))
                {
                    throw new ArgumentException($"The record range {firstId} to {lastId} crosses a cut: {idOf(record)} has {keyOf(record)}, the first record {key}.", nameof(recordsInQueueOrder));
                }
                if (Utc.Normalize(momentOf(record)) >= limit)
                {
                    throw new ArgumentException($"The record range {firstId} to {lastId} reaches the year limit {limit:o}: {idOf(record)} is past it.", nameof(recordsInQueueOrder));
                }
                records.Add(record);
            }
            return new ArchiveFile<TRecord>(key, records, firstMoment, Utc.Normalize(momentOf(recordsInQueueOrder[last])), closedBy, forcedArchive);
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        private sealed class Run<TRecord>
        {
            public Run(ArchiveFileKey key, DateTime firstMoment)
            {
                Key = key;
                FirstMoment = firstMoment;
                LastMoment = firstMoment;
                Limit = YearLimit(firstMoment);
            }

            public ArchiveFileKey Key { get; }

            public List<TRecord> Records { get; } = new List<TRecord>();

            public DateTime FirstMoment { get; }

            /// <summary>The moment of the run's last record in queue order, not the latest moment.</summary>
            public DateTime LastMoment { get; private set; }

            public DateTime Limit { get; }

            public ArchiveClosedBy ClosedBy { get; set; } = ArchiveClosedBy.PeriodEnd;

            public void Add(TRecord record, DateTime moment)
            {
                Records.Add(record);
                LastMoment = moment;
            }
        }
    }
}
