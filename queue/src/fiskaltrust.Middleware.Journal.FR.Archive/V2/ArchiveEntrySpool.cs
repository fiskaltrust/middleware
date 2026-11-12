using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// Keeps the request and response of the entries an export holds in a temporary file, so the memory an
    /// export uses does not grow with the stored content of its period: each entry in memory keeps its scalar
    /// columns only, and loads its request and response from the file when a column needs them. The last
    /// loaded entry is cached, since a file writes all the columns of one entry before the next.
    /// </summary>
    /// <remarks>Not thread-safe: an export reads its entries one at a time. The file is deleted when the spool is disposed.</remarks>
    public sealed class ArchiveEntrySpool : IDisposable
    {
        private readonly FileStream _file;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.Ordinal);
        private long _cachedOffset = -1;
        private ArchiveEntryContent? _cached;

        public ArchiveEntrySpool()
        {
            _file = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, FileOptions.DeleteOnClose);
            _writer = new BinaryWriter(_file, new UTF8Encoding(false), leaveOpen: true);
            _reader = new BinaryReader(_file, new UTF8Encoding(false), leaveOpen: true);
        }

        /// <summary>Reads a stored queue item as <see cref="ArchiveEntry.Read"/> does, and keeps its content in the spool.</summary>
        /// <exception cref="ArchiveEntryException">The item is stored but cannot be read.</exception>
        public ArchiveEntry? Read(Guid queueItemId, Guid queueId, long queueRow, string? request, string? response, string? processingVersion = null)
        {
            var entry = ArchiveEntry.Read(queueItemId, queueId, queueRow, request, response, processingVersion);
            return entry is null ? null : Keep(entry, response!);
        }

        /// <summary>
        /// The entry, its content moved into the spool. <paramref name="response"/> is the response the entry was
        /// read from, kept verbatim.
        /// </summary>
        public ArchiveEntry Keep(ArchiveEntry entry, string response)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            if (response is null) throw new ArgumentNullException(nameof(response));

            var offset = _file.Seek(0, SeekOrigin.End);
            _writer.Write(entry.RawRequest);
            _writer.Write(response);
            _writer.Flush();
            return entry.Spooled(this, offset, Intern);
        }

        internal ArchiveEntryContent Load(long offset)
        {
            if (offset == _cachedOffset && _cached is not null)
            {
                return _cached;
            }
            _file.Seek(offset, SeekOrigin.Begin);
            var request = _reader.ReadString();
            var response = _reader.ReadString();
            _cached = ArchiveEntry.LoadContent(request, response);
            _cachedOffset = offset;
            return _cached;
        }

        /// <summary>One instance per distinct value of the columns most entries repeat (chain, SIRET, serial number, version).</summary>
        private string? Intern(string? value)
        {
            if (value is null)
            {
                return null;
            }
            if (_strings.TryGetValue(value, out var known))
            {
                return known;
            }
            _strings[value] = value;
            return value;
        }

        public void Dispose()
        {
            _reader.Dispose();
            _writer.Dispose();
            _file.Dispose();
        }
    }
}
