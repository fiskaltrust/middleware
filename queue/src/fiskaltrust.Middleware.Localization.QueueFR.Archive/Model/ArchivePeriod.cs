using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Model
{
    /// <summary>
    /// A half-open period [<see cref="Start"/>, <see cref="End"/>) in UTC. Half-open so that consecutive
    /// archives share their boundary without a record falling into both or neither.
    /// </summary>
    public sealed class ArchivePeriod : IEquatable<ArchivePeriod>
    {
        public ArchivePeriod(DateTime start, DateTime end)
        {
            start = Utc.Normalize(start);
            end = Utc.Normalize(end);
            if (end <= start)
            {
                throw new ArgumentException($"The period end ({end:o}) must be after its start ({start:o}).", nameof(end));
            }
            Start = start;
            End = end;
        }

        /// <summary>Inclusive start, UTC.</summary>
        public DateTime Start { get; }

        /// <summary>Exclusive end, UTC.</summary>
        public DateTime End { get; }

        public bool Contains(DateTime moment)
        {
            moment = Utc.Normalize(moment);
            return moment >= Start && moment < End;
        }

        public bool Equals(ArchivePeriod? other) => other is not null && Start == other.Start && End == other.End;

        public override bool Equals(object? obj) => Equals(obj as ArchivePeriod);

        public override int GetHashCode() => unchecked((Start.GetHashCode() * 397) ^ End.GetHashCode());

        public override string ToString() => $"[{Start:o}, {End:o})";
    }

    internal static class Utc
    {
        /// <summary>
        /// Moments are UTC throughout the package. An unspecified kind is taken as UTC (the storage
        /// layers return UTC moments without a kind); a local kind is converted.
        /// </summary>
        public static DateTime Normalize(DateTime moment) => moment.Kind switch
        {
            DateTimeKind.Utc => moment,
            DateTimeKind.Local => moment.ToUniversalTime(),
            _ => DateTime.SpecifyKind(moment, DateTimeKind.Utc)
        };
    }
}
