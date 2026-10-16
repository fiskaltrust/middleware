using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Writing
{
    /// <summary>
    /// Receives the files of one (sub-)archive. Each file is streamed straight into the archive, hashed and
    /// signed on the way, and listed in the manifest; nothing is staged on disk.
    /// </summary>
    public interface IArchiveFileSink
    {
        Task WriteFileAsync(string fileName, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Writes the files of a sub-archive for one archive format version (ADR 0005: a format change splits
    /// the archive, so each version keeps its own writer).
    /// </summary>
    public interface ISubArchiveWriter<TRecord>
    {
        ArchiveFormatVersion FormatVersion { get; }

        Task WriteAsync(SubArchive<TRecord> subArchive, IArchiveFileSink sink, CancellationToken cancellationToken);
    }

    /// <summary>
    /// A sub-archive writer made of CSV files (ADR 0006). The file and column sets are injected: they come
    /// from the field specification of the format version (market-fr#215, #214, #216). An empty
    /// sub-archive still gets every file, with its header only (market-fr#146).
    /// </summary>
    public sealed class CsvSubArchiveWriter<TRecord> : ISubArchiveWriter<TRecord>
    {
        private readonly IReadOnlyList<ICsvFileDefinition<TRecord>> _files;
        private readonly CsvOptions _options;

        public CsvSubArchiveWriter(ArchiveFormatVersion formatVersion, IReadOnlyList<ICsvFileDefinition<TRecord>> files, CsvOptions? options = null)
        {
            FormatVersion = formatVersion ?? throw new ArgumentNullException(nameof(formatVersion));
            if (files is null || files.Count == 0)
            {
                throw new ArgumentException("A sub-archive writer needs at least one file.", nameof(files));
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

        public async Task WriteAsync(SubArchive<TRecord> subArchive, IArchiveFileSink sink, CancellationToken cancellationToken)
        {
            if (subArchive is null) throw new ArgumentNullException(nameof(subArchive));
            if (sink is null) throw new ArgumentNullException(nameof(sink));
            if (!subArchive.Key.FormatVersion.Equals(FormatVersion))
            {
                throw new InvalidOperationException($"Writer for format version {FormatVersion} given a sub-archive of version {subArchive.Key.FormatVersion}.");
            }
            foreach (var file in _files)
            {
                await sink.WriteFileAsync(file.FileName, async (stream, ct) =>
                {
                    using var writer = new StreamWriter(stream, _options.Encoding, 64 * 1024, leaveOpen: true) { NewLine = _options.NewLine };
                    await file.WriteAsync(subArchive.Records, writer, _options, ct).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
