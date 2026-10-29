using System;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Model
{
    /// <summary>
    /// The file handed to the user for one slice of the requested period (at most one fiscal year),
    /// holding one sub-archive per SCU certificate and format version (ADR 0005, glossary "Main archive").
    /// </summary>
    public sealed class MainArchive<TRecord>
    {
        public MainArchive(ArchivePeriod period, IReadOnlyList<SubArchive<TRecord>> subArchives)
        {
            Period = period ?? throw new ArgumentNullException(nameof(period));
            SubArchives = subArchives ?? throw new ArgumentNullException(nameof(subArchives));
        }

        public ArchivePeriod Period { get; }

        public IReadOnlyList<SubArchive<TRecord>> SubArchives { get; }

        /// <summary>True when the period holds no record. The archive is still produced (market-fr#146).</summary>
        public bool IsEmpty => SubArchives.All(s => s.Records.Count == 0);

        /// <summary>Moment of the first record of the period (market-fr#146 "archive start date"), null when empty.</summary>
        public DateTime? FirstRecordMoment => SubArchives.Select(s => s.FirstRecordMoment).Where(m => m.HasValue).Min();

        /// <summary>Moment of the last record of the period (market-fr#146 "archive end date"), null when empty.</summary>
        public DateTime? LastRecordMoment => SubArchives.Select(s => s.LastRecordMoment).Where(m => m.HasValue).Max();
    }

    /// <summary>
    /// The part of a main archive produced under a single SCU certificate and a single archive format
    /// version (glossary "Sub-archive"). Records keep the order the caller supplied (chain order).
    /// </summary>
    public sealed class SubArchive<TRecord>
    {
        public SubArchive(SubArchiveKey key, ArchivePeriod period, IReadOnlyList<TRecord> records, DateTime? firstRecordMoment, DateTime? lastRecordMoment)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Period = period ?? throw new ArgumentNullException(nameof(period));
            Records = records ?? throw new ArgumentNullException(nameof(records));
            FirstRecordMoment = firstRecordMoment;
            LastRecordMoment = lastRecordMoment;
        }

        public SubArchiveKey Key { get; }

        /// <summary>The period of the main archive this sub-archive belongs to.</summary>
        public ArchivePeriod Period { get; }

        public IReadOnlyList<TRecord> Records { get; }

        public DateTime? FirstRecordMoment { get; }

        public DateTime? LastRecordMoment { get; }
    }
}
