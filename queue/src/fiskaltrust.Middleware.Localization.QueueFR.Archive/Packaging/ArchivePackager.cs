using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Signing;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Writing;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Packaging
{
    /// <summary>What every manifest of one generation run carries besides the period (market-fr#133).</summary>
    public sealed class ArchiveMetadata
    {
        public ArchiveMetadata(string siret, DateTime generatedAt, string? documentationUrl = null)
        {
            if (siret is null || siret.Length != 14 || !siret.All(c => c >= '0' && c <= '9'))
            {
                throw new ArgumentException("A SIRET is 14 digits.", nameof(siret));
            }
            Siret = siret;
            GeneratedAt = Utc.Normalize(generatedAt);
            DocumentationUrl = documentationUrl;
        }

        public string Siret { get; }

        public DateTime GeneratedAt { get; }

        public string? DocumentationUrl { get; }
    }

    /// <summary>
    /// Writes a main archive as a signed zip: one signed zip per sub-archive, the archive signing
    /// certificate, and a signed manifest (ADR 0005, ADR 0007, market-fr#133). Everything is streamed
    /// into the output, so no file is staged on disk (unlike the v1.3 ArchiveProcessor, whose temporary
    /// directory handling left the CSV files out of the zip).
    /// </summary>
    public sealed class ArchivePackager<TRecord>
    {
        public const string ManifestFileName = "manifest.json";
        public const string ManifestSignatureFileName = "manifest.sig";
        public const string ArchiveCertificateFileName = "archive-certificate.cer";

        private readonly IReadOnlyDictionary<ArchiveFormatVersion, ISubArchiveWriter<TRecord>> _writers;
        private readonly IArchiveSigner _signer;
        private readonly Func<SubArchiveKey, X509Certificate2?> _scuCertificate;

        /// <param name="writers">One writer per archive format version.</param>
        /// <param name="signer">The archive signer (fiskaltrust certificate, ADR 0007).</param>
        /// <param name="scuCertificate">Optional: the SCU certificate of a sub-archive, embedded as {serial}.cer so that the entry signatures can be checked.</param>
        public ArchivePackager(IEnumerable<ISubArchiveWriter<TRecord>> writers, IArchiveSigner signer, Func<SubArchiveKey, X509Certificate2?>? scuCertificate = null)
        {
            if (writers is null) throw new ArgumentNullException(nameof(writers));
            var map = new Dictionary<ArchiveFormatVersion, ISubArchiveWriter<TRecord>>();
            foreach (var writer in writers)
            {
                if (map.ContainsKey(writer.FormatVersion))
                {
                    throw new ArgumentException($"Two writers for format version {writer.FormatVersion}.", nameof(writers));
                }
                map.Add(writer.FormatVersion, writer);
            }
            _writers = map;
            _signer = signer ?? throw new ArgumentNullException(nameof(signer));
            _scuCertificate = scuCertificate ?? (_ => null);
        }

        public static string SubArchiveFileName(SubArchiveKey key)
        {
            var serial = new string(key.CertificateSerialNumber.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var version = new string(key.FormatVersion.Value.Select(c => char.IsLetterOrDigit(c) || c == '.' ? c : '_').ToArray());
            return $"subarchive_{serial}_v{version}.zip";
        }

        public async Task WriteAsync(MainArchive<TRecord> archive, ArchiveMetadata metadata, Stream output, CancellationToken cancellationToken = default)
        {
            if (archive is null) throw new ArgumentNullException(nameof(archive));
            if (metadata is null) throw new ArgumentNullException(nameof(metadata));
            if (output is null) throw new ArgumentNullException(nameof(output));

            // Fail before writing anything when a writer is missing.
            var missing = archive.SubArchives.Select(s => s.Key.FormatVersion).FirstOrDefault(v => !_writers.ContainsKey(v));
            if (missing is not null)
            {
                throw new InvalidOperationException($"No sub-archive writer for archive format version {missing}.");
            }
            var duplicateName = archive.SubArchives.GroupBy(s => SubArchiveFileName(s.Key), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicateName is not null)
            {
                throw new InvalidOperationException($"Two sub-archives map to the file name {duplicateName.Key}.");
            }

            var manifest = NewManifest(ArchiveManifest.MainKind, archive.Period, metadata);
            manifest.FirstRecordMoment = archive.FirstRecordMoment;
            manifest.LastRecordMoment = archive.LastRecordMoment;
            manifest.RecordCount = archive.SubArchives.Sum(s => s.Records.Count);
            manifest.SubArchives = new List<ManifestSubArchive>();

            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                var sink = new ZipFileSink(zip, _signer, manifest, metadata.GeneratedAt);
                foreach (var subArchive in archive.SubArchives)
                {
                    var fileName = SubArchiveFileName(subArchive.Key);
                    await sink.WriteFileAsync(fileName, (stream, ct) => WriteSubArchiveAsync(subArchive, metadata, stream, ct), cancellationToken).ConfigureAwait(false);
                    manifest.SubArchives.Add(new ManifestSubArchive
                    {
                        FileName = fileName,
                        CertificateSerialNumber = subArchive.Key.CertificateSerialNumber,
                        FormatVersion = subArchive.Key.FormatVersion.Value,
                        RecordCount = subArchive.Records.Count
                    });
                }
                await FinishAsync(sink, manifest, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteSubArchiveAsync(SubArchive<TRecord> subArchive, ArchiveMetadata metadata, Stream output, CancellationToken cancellationToken)
        {
            var manifest = NewManifest(ArchiveManifest.SubKind, subArchive.Period, metadata);
            manifest.CertificateSerialNumber = subArchive.Key.CertificateSerialNumber;
            manifest.FormatVersion = subArchive.Key.FormatVersion.Value;
            manifest.FirstRecordMoment = subArchive.FirstRecordMoment;
            manifest.LastRecordMoment = subArchive.LastRecordMoment;
            manifest.RecordCount = subArchive.Records.Count;

            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            var sink = new ZipFileSink(zip, _signer, manifest, metadata.GeneratedAt);
            await _writers[subArchive.Key.FormatVersion].WriteAsync(subArchive, sink, cancellationToken).ConfigureAwait(false);

            var scuCertificate = _scuCertificate(subArchive.Key);
            if (scuCertificate is not null)
            {
                var name = Path.GetFileNameWithoutExtension(SubArchiveFileName(subArchive.Key)).Replace("subarchive_", "scu_") + ".cer";
                await sink.WriteBytesAsync(name, scuCertificate.RawData, cancellationToken).ConfigureAwait(false);
            }
            await FinishAsync(sink, manifest, cancellationToken).ConfigureAwait(false);
        }

        private ArchiveManifest NewManifest(string kind, ArchivePeriod period, ArchiveMetadata metadata)
        {
            var certificate = _signer.Certificate;
            using var sha256 = SHA256.Create();
            return new ArchiveManifest
            {
                Kind = kind,
                Siret = metadata.Siret,
                DocumentationUrl = metadata.DocumentationUrl,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
                GeneratedAt = metadata.GeneratedAt,
                SignatureAlgorithm = _signer.SignatureAlgorithm,
                SigningCertificate = new ManifestCertificate
                {
                    FileName = ArchiveCertificateFileName,
                    SerialNumber = certificate.SerialNumber,
                    Subject = certificate.Subject,
                    Issuer = certificate.Issuer,
                    Sha256Thumbprint = Convert.ToBase64String(sha256.ComputeHash(certificate.RawData))
                }
            };
        }

        private async Task FinishAsync(ZipFileSink sink, ArchiveManifest manifest, CancellationToken cancellationToken)
        {
            await sink.WriteBytesAsync(ArchiveCertificateFileName, _signer.Certificate.RawData, cancellationToken).ConfigureAwait(false);
            var manifestBytes = ManifestSerializer.Serialize(manifest);
            byte[] manifestHash;
            using (var sha256 = SHA256.Create())
            {
                manifestHash = sha256.ComputeHash(manifestBytes);
            }
            var signature = await _signer.SignHashAsync(manifestHash, cancellationToken).ConfigureAwait(false);
            await sink.WriteUnlistedAsync(ManifestFileName, manifestBytes, cancellationToken).ConfigureAwait(false);
            await sink.WriteUnlistedAsync(ManifestSignatureFileName, System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(signature)), cancellationToken).ConfigureAwait(false);
        }

        private sealed class ZipFileSink : IArchiveFileSink
        {
            private readonly ZipArchive _zip;
            private readonly IArchiveSigner _signer;
            private readonly ArchiveManifest _manifest;
            private readonly DateTimeOffset _timestamp;
            private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public ZipFileSink(ZipArchive zip, IArchiveSigner signer, ArchiveManifest manifest, DateTime generatedAt)
            {
                _zip = zip;
                _signer = signer;
                _manifest = manifest;
                // Zip timestamps are local wall-clock with 2-second precision; the manifest holds the exact moment.
                _timestamp = new DateTimeOffset(DateTime.SpecifyKind(generatedAt, DateTimeKind.Utc));
            }

            public async Task WriteFileAsync(string fileName, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(new[] { '/', '\\' }) >= 0
                    || string.Equals(fileName, ManifestFileName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fileName, ManifestSignatureFileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"'{fileName}' is not a valid archive file name.", nameof(fileName));
                }
                if (write is null) throw new ArgumentNullException(nameof(write));

                byte[] hash;
                long size;
                using (var entryStream = Open(fileName))
                using (var hashing = new HashingStream(entryStream, leaveOpen: true))
                {
                    await write(hashing, cancellationToken).ConfigureAwait(false);
                    await hashing.FlushAsync(cancellationToken).ConfigureAwait(false);
                    hash = hashing.GetHash();
                    size = hashing.BytesWritten;
                }
                var signature = await _signer.SignHashAsync(hash, cancellationToken).ConfigureAwait(false);
                _manifest.Files.Add(new ManifestFile
                {
                    Name = fileName,
                    Size = size,
                    Hash = Convert.ToBase64String(hash),
                    Signature = Convert.ToBase64String(signature)
                });
            }

            public Task WriteBytesAsync(string fileName, byte[] content, CancellationToken cancellationToken)
                => WriteFileAsync(fileName, (s, ct) => s.WriteAsync(content, 0, content.Length, ct), cancellationToken);

            public async Task WriteUnlistedAsync(string fileName, byte[] content, CancellationToken cancellationToken)
            {
                using var entryStream = Open(fileName);
                await entryStream.WriteAsync(content, 0, content.Length, cancellationToken).ConfigureAwait(false);
            }

            private Stream Open(string fileName)
            {
                if (!_names.Add(fileName))
                {
                    throw new InvalidOperationException($"The archive already holds a file named {fileName}.");
                }
                var entry = _zip.CreateEntry(fileName, CompressionLevel.Optimal);
                entry.LastWriteTime = _timestamp;
                return entry.Open();
            }
        }
    }
}
