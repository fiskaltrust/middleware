using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Csv
{
    /// <summary>One column: its header and how a row yields the cell value.</summary>
    public sealed class CsvColumn<TRow>
    {
        public CsvColumn(string name, Func<TRow, object?> value, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A column needs a name.", nameof(name));
            }
            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Description = description ?? "";
        }

        public string Name { get; }

        /// <summary>What the column holds, in French, for the archive documentation (archive-v2 section 11).</summary>
        public string Description { get; }

        public Func<TRow, object?> Value { get; }
    }

    /// <summary>
    /// One CSV file of a sub-archive, built from the sub-archive's records. This is the extension point for
    /// the column sets: the files and fields of each archive format version are defined by the field
    /// specification (market-fr#215, #214, #216), not by this package.
    /// </summary>
    /// <summary>A file the archive documentation describes: what one row is, and each column.</summary>
    public interface IDocumentedFile
    {
        string FileName { get; }

        /// <summary>What one row of the file is, in French.</summary>
        string Purpose { get; }

        IReadOnlyList<(string Name, string Description)> ColumnDocs { get; }
    }

    public interface ICsvFileDefinition<TRecord>
    {
        /// <summary>File name inside the sub-archive, e.g. "receipts.csv".</summary>
        string FileName { get; }

        Task WriteAsync(IReadOnlyList<TRecord> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken);
    }

    /// <summary>
    /// A flat file: one row per record (e.g. one row per receipt). <c>include</c> selects the records of the
    /// file, e.g. one journal-{chain}.csv per receipt chain (archive-v2 field contract, market-fr#227).
    /// </summary>
    public sealed class FlatCsvFile<TRecord> : ICsvFileDefinition<TRecord>, IDocumentedFile
    {
        private readonly IReadOnlyList<CsvColumn<TRecord>> _columns;
        private readonly Func<TRecord, bool> _include;

        public FlatCsvFile(string fileName, IReadOnlyList<CsvColumn<TRecord>> columns, Func<TRecord, bool>? include = null, string? purpose = null)
        {
            _include = include ?? (_ => true);
            FileName = CsvFileName.Validate(fileName);
            _columns = CsvFileName.ValidateColumns(columns);
            Purpose = purpose ?? "";
        }

        public string FileName { get; }

        public string Purpose { get; }

        public IReadOnlyList<(string Name, string Description)> ColumnDocs => _columns.Select(c => (c.Name, c.Description)).ToList();

        public async Task WriteAsync(IReadOnlyList<TRecord> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            var csv = new CsvWriter(writer, options);
            if (options.WriteHeader)
            {
                await csv.WriteRowAsync(_columns.Select(c => c.Name), cancellationToken).ConfigureAwait(false);
            }
            foreach (var record in records.Where(_include))
            {
                await csv.WriteRowAsync(_columns.Select(c => CsvValueFormatter.Format(c.Value(record))), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>A row of a one-to-many file: the parent record, the child, and the child's 0-based position.</summary>
    public sealed class ChildRow<TRecord, TChild>
    {
        public ChildRow(TRecord parent, TChild child, int index)
        {
            Parent = parent;
            Child = child;
            Index = index;
        }

        public TRecord Parent { get; }

        public TChild Child { get; }

        public int Index { get; }
    }

    /// <summary>
    /// A one-to-many file: one row per child of each record (e.g. one row per charge item), joined to the
    /// flat file on a parent column such as the queue item id (ADR 0006).
    /// </summary>
    public sealed class OneToManyCsvFile<TRecord, TChild> : ICsvFileDefinition<TRecord>, IDocumentedFile
    {
        private readonly Func<TRecord, IEnumerable<TChild>?> _children;
        private readonly IReadOnlyList<CsvColumn<ChildRow<TRecord, TChild>>> _columns;
        private readonly Func<TRecord, bool> _include;

        public OneToManyCsvFile(string fileName, Func<TRecord, IEnumerable<TChild>?> children, IReadOnlyList<CsvColumn<ChildRow<TRecord, TChild>>> columns, Func<TRecord, bool>? include = null, string? purpose = null)
        {
            _include = include ?? (_ => true);
            FileName = CsvFileName.Validate(fileName);
            _children = children ?? throw new ArgumentNullException(nameof(children));
            _columns = CsvFileName.ValidateColumns(columns);
            Purpose = purpose ?? "";
        }

        public string FileName { get; }

        public string Purpose { get; }

        public IReadOnlyList<(string Name, string Description)> ColumnDocs => _columns.Select(c => (c.Name, c.Description)).ToList();

        public async Task WriteAsync(IReadOnlyList<TRecord> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            var csv = new CsvWriter(writer, options);
            if (options.WriteHeader)
            {
                await csv.WriteRowAsync(_columns.Select(c => c.Name), cancellationToken).ConfigureAwait(false);
            }
            foreach (var record in records.Where(_include))
            {
                var index = 0;
                foreach (var child in _children(record) ?? Enumerable.Empty<TChild>())
                {
                    var row = new ChildRow<TRecord, TChild>(record, child, index++);
                    await csv.WriteRowAsync(_columns.Select(c => CsvValueFormatter.Format(c.Value(row))), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    internal static class CsvFileName
    {
        public static string Validate(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(new[] { '/', '\\' }) >= 0 || fileName.StartsWith("manifest.", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"'{fileName}' is not a valid archive file name.", nameof(fileName));
            }
            return fileName;
        }

        public static IReadOnlyList<CsvColumn<T>> ValidateColumns<T>(IReadOnlyList<CsvColumn<T>> columns)
        {
            if (columns is null || columns.Count == 0)
            {
                throw new ArgumentException("A CSV file needs at least one column.", nameof(columns));
            }
            var duplicate = columns.GroupBy(c => c.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new ArgumentException($"Duplicate column '{duplicate.Key}'.", nameof(columns));
            }
            return columns;
        }
    }
}
