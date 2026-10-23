using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Split
{
    public sealed class ArchiveSplitOptions
    {
        /// <summary>Where the fiscal year starts. Defaults to the calendar year.</summary>
        public FiscalYear FiscalYear { get; set; } = FiscalYear.Calendar;

        /// <summary>
        /// The time zone whose wall clock the fiscal-year boundaries are read in. Defaults to UTC.
        /// Provisional: whether the cut is at local midnight (Europe/Paris, or the overseas zone of the
        /// outlet) is an open point on market-fr#218.
        /// </summary>
        public TimeZoneInfo BoundaryTimeZone { get; set; } = TimeZoneInfo.Utc;

        /// <summary>
        /// The key used for an empty period when no record of the request gives one (see
        /// <see cref="ArchiveSplitter.Split{TRecord}"/>). When null, such a period gets no sub-archive.
        /// </summary>
        public SubArchiveKey? DefaultKey { get; set; }
    }

    /// <summary>
    /// Cuts a requested period into main archives at the fiscal-year boundaries (market-fr#146), and each
    /// main archive into sub-archives by SCU certificate serial number × format version (ADR 0005).
    /// </summary>
    public static class ArchiveSplitter
    {
        /// <summary>The fiscal-year slices of <paramref name="requested"/>, in order.</summary>
        public static IReadOnlyList<ArchivePeriod> Slices(ArchivePeriod requested, ArchiveSplitOptions? options = null)
        {
            if (requested is null)
            {
                throw new ArgumentNullException(nameof(requested));
            }
            options ??= new ArchiveSplitOptions();
            var slices = new List<ArchivePeriod>();
            var cursor = requested.Start;
            while (cursor < requested.End)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(cursor, options.BoundaryTimeZone);
                var nextStartLocal = options.FiscalYear.StartOfYearContaining(local).AddYears(1);
                var nextBoundary = ToUtc(nextStartLocal, options.BoundaryTimeZone);
                var sliceEnd = nextBoundary < requested.End ? nextBoundary : requested.End;
                slices.Add(new ArchivePeriod(cursor, sliceEnd));
                cursor = sliceEnd;
            }
            return slices;
        }

        /// <summary>
        /// Splits <paramref name="records"/> (supplied in chain order) over <paramref name="requested"/>.
        /// Records outside the requested period are ignored. Every fiscal-year slice yields a main archive,
        /// including slices without records (market-fr#146): such a slice gets one empty sub-archive keyed on
        /// the last key seen before it, else the first key seen after it, else
        /// <see cref="ArchiveSplitOptions.DefaultKey"/>, else no sub-archive at all.
        /// </summary>
        public static IReadOnlyList<MainArchive<TRecord>> Split<TRecord>(
            IEnumerable<TRecord> records,
            Func<TRecord, DateTime> momentOf,
            Func<TRecord, SubArchiveKey> keyOf,
            ArchivePeriod requested,
            ArchiveSplitOptions? options = null)
        {
            if (records is null) throw new ArgumentNullException(nameof(records));
            if (momentOf is null) throw new ArgumentNullException(nameof(momentOf));
            if (keyOf is null) throw new ArgumentNullException(nameof(keyOf));
            options ??= new ArchiveSplitOptions();

            var slices = Slices(requested, options);
            var buckets = slices.Select(_ => new SliceBucket<TRecord>()).ToArray();
            var starts = slices.Select(s => s.Start).ToArray();

            foreach (var record in records)
            {
                var moment = Utc.Normalize(momentOf(record));
                if (!requested.Contains(moment))
                {
                    continue;
                }
                var index = Array.BinarySearch(starts, moment);
                if (index < 0)
                {
                    index = ~index - 1;
                }
                buckets[index].Add(keyOf(record), record, moment);
            }

            var result = new List<MainArchive<TRecord>>(slices.Count);
            SubArchiveKey? lastKeySeen = null;
            for (var i = 0; i < slices.Count; i++)
            {
                var bucket = buckets[i];
                if (bucket.Groups.Count > 0)
                {
                    result.Add(new MainArchive<TRecord>(slices[i], bucket.Groups.Select(g => g.ToSubArchive(slices[i])).ToList()));
                    lastKeySeen = bucket.Groups[bucket.Groups.Count - 1].Key;
                    continue;
                }

                var key = lastKeySeen
                          ?? buckets.Skip(i + 1).Where(b => b.Groups.Count > 0).Select(b => b.Groups[0].Key).FirstOrDefault()
                          ?? options.DefaultKey;
                var subArchives = key is null
                    ? new List<SubArchive<TRecord>>()
                    : new List<SubArchive<TRecord>> { new SubArchive<TRecord>(key, slices[i], Array.Empty<TRecord>(), null, null) };
                result.Add(new MainArchive<TRecord>(slices[i], subArchives));
            }
            return result;
        }

        private static DateTime ToUtc(DateTime localWallClock, TimeZoneInfo zone)
        {
            // A boundary that falls in a daylight-saving gap does not exist; take the first valid instant after it.
            while (zone.IsInvalidTime(localWallClock))
            {
                localWallClock = localWallClock.AddMinutes(30);
            }
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(localWallClock, zone), DateTimeKind.Utc);
        }

        private sealed class SliceBucket<TRecord>
        {
            private readonly Dictionary<SubArchiveKey, Group<TRecord>> _byKey = new Dictionary<SubArchiveKey, Group<TRecord>>();

            public List<Group<TRecord>> Groups { get; } = new List<Group<TRecord>>();

            public void Add(SubArchiveKey key, TRecord record, DateTime moment)
            {
                if (key is null)
                {
                    throw new ArgumentException("Every record needs a sub-archive key.", nameof(key));
                }
                if (!_byKey.TryGetValue(key, out var group))
                {
                    group = new Group<TRecord>(key);
                    _byKey.Add(key, group);
                    Groups.Add(group);
                }
                group.Add(record, moment);
            }
        }

        private sealed class Group<TRecord>
        {
            private readonly List<TRecord> _records = new List<TRecord>();
            private DateTime? _first;
            private DateTime? _last;

            public Group(SubArchiveKey key) => Key = key;

            public SubArchiveKey Key { get; }

            public void Add(TRecord record, DateTime moment)
            {
                _records.Add(record);
                if (_first is null || moment < _first) _first = moment;
                if (_last is null || moment > _last) _last = moment;
            }

            public SubArchive<TRecord> ToSubArchive(ArchivePeriod period) => new SubArchive<TRecord>(Key, period, _records, _first, _last);
        }
    }
}
