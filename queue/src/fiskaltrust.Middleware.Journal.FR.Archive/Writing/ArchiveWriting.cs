using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Writing
{
    /// <summary>
    /// Receives the files of one archive file. Each file is streamed straight into the archive, hashed and
    /// signed on the way, and listed in the manifest; nothing is staged on disk.
    /// </summary>
    public interface IArchiveFileSink
    {
        Task WriteFileAsync(string fileName, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Writes the data files of an archive file for one archive format version (a format change ends the file,
    /// ADR 0019, so each version keeps its own writer).
    /// </summary>
    public interface IArchiveFileWriter<TRecord>
    {
        ArchiveFormatVersion FormatVersion { get; }

        Task WriteAsync(ArchiveFile<TRecord> file, IArchiveFileSink sink, CancellationToken cancellationToken);
    }

    /// <summary>
    /// An archive file writer made of CSV files (ADR 0006). The file and column sets are injected: they come
    /// from the field specification of the format version (market-fr#215, #214, #216). A CSV file without
    /// rows still gets its header (market-fr#146).
    /// </summary>
    public sealed class CsvArchiveWriter<TRecord> : IArchiveFileWriter<TRecord>
    {
        private readonly IReadOnlyList<ICsvFileDefinition<TRecord>> _files;
        private readonly CsvOptions _options;

        public CsvArchiveWriter(ArchiveFormatVersion formatVersion, IReadOnlyList<ICsvFileDefinition<TRecord>> files, CsvOptions? options = null)
        {
            FormatVersion = formatVersion ?? throw new ArgumentNullException(nameof(formatVersion));
            if (files is null || files.Count == 0)
            {
                throw new ArgumentException("An archive file writer needs at least one file.", nameof(files));
            }
            var duplicate = files.GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new ArgumentException($"Duplicate file '{duplicate.Key}'.", nameof(files));
            }
            _files = files;
            _options = options ?? new CsvOptions();
        }

        public ArchiveFormatVersion FormatVersion { get; }

        public async Task WriteAsync(ArchiveFile<TRecord> file, IArchiveFileSink sink, CancellationToken cancellationToken)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (sink is null) throw new ArgumentNullException(nameof(sink));
            if (!file.Key.FormatVersion.Equals(FormatVersion))
            {
                throw new InvalidOperationException($"Writer for format version {FormatVersion} given an archive file of version {file.Key.FormatVersion}.");
            }
            foreach (var csv in _files)
            {
                await sink.WriteFileAsync(csv.FileName, async (stream, ct) =>
                {
                    using var writer = new StreamWriter(stream, _options.Encoding, 64 * 1024, leaveOpen: true) { NewLine = _options.NewLine };
                    await csv.WriteAsync(file.Records, writer, _options, ct).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
