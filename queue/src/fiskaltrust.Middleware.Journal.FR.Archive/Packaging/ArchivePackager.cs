using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Writing;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Packaging
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
    /// <remarks>
    /// Per-SCU signing (market-fr#262): each sub-archive is signed with the key of the SCU whose
    /// certificate it embeds, and the main archive's manifest, the signed index of its sub-archives, with
    /// the key of the newest sub-archive's SCU. Every signer is resolved before anything is written, so an
    /// export that lacks one SCU's key fails without output.
    /// </remarks>
    public sealed class ArchivePackager<TRecord>
    {
        public const string ManifestFileName = "manifest.json";
        public const string ManifestSignatureFileName = "manifest.sig";
        public const string ArchiveCertificateFileName = "archive-certificate.cer";

        private readonly IReadOnlyDictionary<ArchiveFormatVersion, ISubArchiveWriter<TRecord>> _writers;
        private readonly Func<SubArchiveKey, IArchiveSigner> _signerFor;
        private readonly IArchiveSigner _emptyArchiveSigner;
        private readonly Func<SubArchiveKey, IArchiveSigner, X509Certificate2?> _scuCertificate;
        private readonly Func<SubArchive<TRecord>, IReadOnlyList<ManifestChainAnchor>>? _chainAnchors;
        private readonly Func<SubArchive<TRecord>, string?>? _schemaOf;

        /// <param name="writers">One writer per archive format version.</param>
        /// <param name="signer">The one archive signer of every sub-archive and of the main archive (fiskaltrust certificate, ADR 0007).</param>
        /// <param name="scuCertificate">Optional: the SCU certificate of a sub-archive, embedded as {serial}.cer so that the entry signatures can be checked.</param>
        public ArchivePackager(IEnumerable<ISubArchiveWriter<TRecord>> writers, IArchiveSigner signer, Func<SubArchiveKey, X509Certificate2?>? scuCertificate = null)
            : this(writers, signer ?? throw new ArgumentNullException(nameof(signer)), _ => signer, (key, _) => scuCertificate?.Invoke(key), null, null)
        {
        }

        /// <param name="writers">One writer per archive format version.</param>
        /// <param name="signerFor">
        /// The signer of a sub-archive: the SCU whose certificate signed its receipts (market-fr#262). It throws when
        /// that SCU's key is not available here; the export then fails before anything is written.
        /// </param>
        /// <param name="emptyArchiveSigner">Signs a main archive without any sub-archive: the current SCU.</param>
        /// <param name="chainAnchors">Optional: per chain, the first entry of a sub-archive and the signature it covers.</param>
        /// <param name="schemaOf">Optional: the column schema of a sub-archive, <c>InfoCert</c> or <c>LNE</c>.</param>
        public ArchivePackager(IEnumerable<ISubArchiveWriter<TRecord>> writers, Func<SubArchiveKey, IArchiveSigner> signerFor, IArchiveSigner emptyArchiveSigner,
            Func<SubArchive<TRecord>, IReadOnlyList<ManifestChainAnchor>>? chainAnchors = null, Func<SubArchive<TRecord>, string?>? schemaOf = null)
            : this(writers, emptyArchiveSigner, signerFor, (_, signer) => signer.Certificate, chainAnchors, schemaOf)
        {
        }

        private ArchivePackager(IEnumerable<ISubArchiveWriter<TRecord>> writers, IArchiveSigner emptyArchiveSigner, Func<SubArchiveKey, IArchiveSigner> signerFor,
            Func<SubArchiveKey, IArchiveSigner, X509Certificate2?> scuCertificate, Func<SubArchive<TRecord>, IReadOnlyList<ManifestChainAnchor>>? chainAnchors,
            Func<SubArchive<TRecord>, string?>? schemaOf)
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
            _emptyArchiveSigner = emptyArchiveSigner ?? throw new ArgumentNullException(nameof(emptyArchiveSigner));
            _signerFor = signerFor ?? throw new ArgumentNullException(nameof(signerFor));
            _scuCertificate = scuCertificate;
            _chainAnchors = chainAnchors;
            _schemaOf = schemaOf;
        }

        /// <summary>
        /// <c>subarchive_<serial>_v<version>_<run>.zip</c> (market-fr#262): the run number tells apart the sub-archives of an
        /// SCU switched out and back in within one main archive.
        /// </summary>
        public static string SubArchiveFileName(SubArchiveKey key, int run)
        {
            var serial = new string(key.CertificateSerialNumber.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var version = new string(key.FormatVersion.Value.Select(c => char.IsLetterOrDigit(c) || c == '.' ? c : '_').ToArray());
            return $"subarchive_{serial}_v{version}_{run.ToString(System.Globalization.CultureInfo.InvariantCulture)}.zip";
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
            var duplicateName = archive.SubArchives.GroupBy(s => SubArchiveFileName(s.Key, s.Run), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicateName is not null)
            {
                throw new InvalidOperationException($"Two sub-archives map to the file name {duplicateName.Key}.");
            }

            // Resolve every signer first: an export missing one SCU's key must fail before writing anything.
            var signers = archive.SubArchives.ToDictionary(s => s, s => _signerFor(s.Key) ?? throw new InvalidOperationException($"No archive signer for the SCU certificate {s.Key.CertificateSerialNumber}."));
            var newest = archive.SubArchives
                .OrderBy(s => s.LastRecordMoment ?? s.Period.End)
                .ThenBy(s => s.Period.End)
                .LastOrDefault();
            var mainSigner = newest is null ? _emptyArchiveSigner : signers[newest];

            var manifest = NewManifest(ArchiveManifest.MainKind, archive.Period, metadata, mainSigner);
            manifest.FirstRecordMoment = archive.FirstRecordMoment;
            manifest.LastRecordMoment = archive.LastRecordMoment;
            manifest.RecordCount = archive.SubArchives.Sum(s => s.Records.Count);
            manifest.SubArchives = new List<ManifestSubArchive>();
            manifest.Schema = newest is null ? null : _schemaOf?.Invoke(newest);

            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                var sink = new ZipFileSink(zip, mainSigner, manifest, metadata.GeneratedAt);
                foreach (var subArchive in archive.SubArchives)
                {
                    var fileName = SubArchiveFileName(subArchive.Key, subArchive.Run);
                    await sink.WriteFileAsync(fileName, (stream, ct) => WriteSubArchiveAsync(subArchive, signers[subArchive], metadata, stream, ct), cancellationToken).ConfigureAwait(false);
                    manifest.SubArchives.Add(new ManifestSubArchive
                    {
                        FileName = fileName,
                        CertificateSerialNumber = subArchive.Key.CertificateSerialNumber,
                        Run = subArchive.Run,
                        FormatVersion = subArchive.Key.FormatVersion.Value,
                        Schema = _schemaOf?.Invoke(subArchive),
                        RecordCount = subArchive.Records.Count,
                        PeriodStart = subArchive.Period.Start,
                        PeriodEnd = subArchive.Period.End,
                        FirstRecordMoment = subArchive.FirstRecordMoment,
                        LastRecordMoment = subArchive.LastRecordMoment,
                        Hash = manifest.Files.Last(f => f.Name == fileName).Hash
                    });
                }
                await FinishAsync(sink, manifest, mainSigner, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteSubArchiveAsync(SubArchive<TRecord> subArchive, IArchiveSigner signer, ArchiveMetadata metadata, Stream output, CancellationToken cancellationToken)
        {
            var manifest = NewManifest(ArchiveManifest.SubKind, subArchive.Period, metadata, signer);
            manifest.CertificateSerialNumber = subArchive.Key.CertificateSerialNumber;
            manifest.Run = subArchive.Run;
            manifest.FormatVersion = subArchive.Key.FormatVersion.Value;
            manifest.FirstRecordMoment = subArchive.FirstRecordMoment;
            manifest.LastRecordMoment = subArchive.LastRecordMoment;
            manifest.RecordCount = subArchive.Records.Count;
            manifest.Schema = _schemaOf?.Invoke(subArchive);
            if (_chainAnchors is not null)
            {
                manifest.ChainAnchors = _chainAnchors(subArchive).ToList();
            }

            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            var sink = new ZipFileSink(zip, signer, manifest, metadata.GeneratedAt);
            await _writers[subArchive.Key.FormatVersion].WriteAsync(subArchive, sink, cancellationToken).ConfigureAwait(false);

            var scuCertificate = _scuCertificate(subArchive.Key, signer);
            if (scuCertificate is not null)
            {
                var name = Path.GetFileNameWithoutExtension(SubArchiveFileName(subArchive.Key, subArchive.Run)).Replace("subarchive_", "scu_") + ".cer";
                await sink.WriteBytesAsync(name, scuCertificate.RawData, cancellationToken).ConfigureAwait(false);
            }
            await FinishAsync(sink, manifest, signer, cancellationToken).ConfigureAwait(false);
        }

        private static ArchiveManifest NewManifest(string kind, ArchivePeriod period, ArchiveMetadata metadata, IArchiveSigner signer)
        {
            var certificate = signer.Certificate;
            using var sha256 = SHA256.Create();
            return new ArchiveManifest
            {
                Kind = kind,
                Siret = metadata.Siret,
                DocumentationUrl = metadata.DocumentationUrl,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
                GeneratedAt = metadata.GeneratedAt,
                SignatureAlgorithm = signer.SignatureAlgorithm,
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

        private static async Task FinishAsync(ZipFileSink sink, ArchiveManifest manifest, IArchiveSigner signer, CancellationToken cancellationToken)
        {
            await sink.WriteBytesAsync(ArchiveCertificateFileName, signer.Certificate.RawData, cancellationToken).ConfigureAwait(false);
            var manifestBytes = ManifestSerializer.Serialize(manifest);
            byte[] manifestHash;
            using (var sha256 = SHA256.Create())
            {
                manifestHash = sha256.ComputeHash(manifestBytes);
            }
            var signature = await signer.SignHashAsync(manifestHash, cancellationToken).ConfigureAwait(false);
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
