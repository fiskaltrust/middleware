using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Model
{
    /// <summary>
    /// The version of a sub-archive's layout: which files it holds and which columns they have.
    /// A half migration switches a queue from one format version to the next, and the main archive then
    /// holds one sub-archive per format version (ADR 0005).
    /// </summary>
    public sealed class ArchiveFormatVersion : IEquatable<ArchiveFormatVersion>
    {
        /// <summary>The v1.x archive (queueItems/receiptJournals CSV with JSON in columns).</summary>
        public static readonly ArchiveFormatVersion V1 = new ArchiveFormatVersion("1");

        /// <summary>The normalised CSV archive of QueueFR v2 (ADR 0006).</summary>
        public static readonly ArchiveFormatVersion V2 = new ArchiveFormatVersion("2");

        public ArchiveFormatVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A format version must not be empty.", nameof(value));
            }
            Value = value;
        }

        public string Value { get; }

        public bool Equals(ArchiveFormatVersion? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as ArchiveFormatVersion);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value;
    }
}
