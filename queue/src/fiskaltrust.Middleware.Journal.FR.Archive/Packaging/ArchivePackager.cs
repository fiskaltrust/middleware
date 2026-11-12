using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Writing;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Packaging
{
    /// <summary>What every manifest of one generation run carries besides the file's own data (market-fr#133).</summary>
    public sealed class ArchiveMetadata
    {
        public ArchiveMetadata(string siret, DateTime generatedAt, string? documentationUrl = null)
        {
            if (siret is null || siret.Length != 14 || !siret.All(c => c >= '0' && c <= '9'))
            {
                throw new ArgumentException($"A SIRET is 14 digits; '{siret}' is not.", nameof(siret));
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
    /// Writes one archive file as a standalone signed zip (ADR 0019, contract section 10): its data files, the SCU
    /// certificate <c>archive-certificate.cer</c>, and <c>manifest.json</c> signed in <c>manifest.sig</c>. Everything is
    /// streamed into the output, so no file is staged on disk.
    /// </summary>
    /// <remarks>
    /// The file is signed with the key of the SCU whose certificate signed its entries (market-fr#262, ADR 0019), found by
    /// the certificate serial of the entries. The signer is resolved before anything is written, so a file whose key is
    /// missing fails without output.
    /// </remarks>
    public sealed class ArchivePackager<TRecord>
    {
        public const string ManifestFileName = "manifest.json";
        public const string ManifestSignatureFileName = "manifest.sig";
        public const string ArchiveCertificateFileName = "archive-certificate.cer";

        private readonly IReadOnlyDictionary<ArchiveFormatVersion, IArchiveFileWriter<TRecord>> _writers;
        private readonly Func<ArchiveFileKey, IArchiveSigner> _signerFor;
        private readonly Func<TRecord, ManifestRecord> _describe;
        private readonly Func<ArchiveFile<TRecord>, IReadOnlyList<ManifestChainAnchor>>? _chainAnchors;
        private readonly Func<ArchiveFile<TRecord>, string?>? _schemaOf;

        /// <param name="writers">One writer per archive format version.</param>
        /// <param name="signerFor">
        /// The signer of a file: the SCU whose certificate signed its entries (market-fr#262). It throws when that SCU's key
        /// is not available here; the file then fails before anything is written.
        /// </param>
        /// <param name="describe">How the manifest names a record (<c>firstRecord</c>, <c>lastRecord</c>).</param>
        /// <param name="chainAnchors">Per chain, the file's first entry and the signature it covers.</param>
        /// <param name="schemaOf">The column schema of a file, <c>InfoCert</c> or <c>LNE</c>.</param>
        public ArchivePackager(IEnumerable<IArchiveFileWriter<TRecord>> writers, Func<ArchiveFileKey, IArchiveSigner> signerFor, Func<TRecord, ManifestRecord> describe,
            Func<ArchiveFile<TRecord>, IReadOnlyList<ManifestChainAnchor>>? chainAnchors = null, Func<ArchiveFile<TRecord>, string?>? schemaOf = null)
        {
            if (writers is null) throw new ArgumentNullException(nameof(writers));
            var map = new Dictionary<ArchiveFormatVersion, IArchiveFileWriter<TRecord>>();
            foreach (var writer in writers)
            {
                if (map.ContainsKey(writer.FormatVersion))
                {
                    throw new ArgumentException($"Two writers for format version {writer.FormatVersion}.", nameof(writers));
                }
                map.Add(writer.FormatVersion, writer);
            }
            _writers = map;
            _signerFor = signerFor ?? throw new ArgumentNullException(nameof(signerFor));
            _describe = describe ?? throw new ArgumentNullException(nameof(describe));
            _chainAnchors = chainAnchors;
            _schemaOf = schemaOf;
        }

        /// <summary>With one signer for every file, e.g. a test certificate or an injected signer.</summary>
        public ArchivePackager(IEnumerable<IArchiveFileWriter<TRecord>> writers, IArchiveSigner signer, Func<TRecord, ManifestRecord> describe,
            Func<ArchiveFile<TRecord>, IReadOnlyList<ManifestChainAnchor>>? chainAnchors = null, Func<ArchiveFile<TRecord>, string?>? schemaOf = null)
            : this(writers, signer is null ? throw new ArgumentNullException(nameof(signer)) : _ => signer, describe, chainAnchors, schemaOf)
        {
        }

        /// <summary>
        /// <c>archive_&lt;serial&gt;_&lt;firstMoment&gt;_&lt;lastMoment&gt;.zip</c> (contract section 10): the moments of the file's
        /// first and last record as <c>yyyyMMddTHHmmssfffffffZ</c> (UTC, to the tick, so two files never share a name); a
        /// character of the serial that is not a letter, a digit or <c>-</c> becomes <c>_</c>.
        /// </summary>
        public static string FileName(ArchiveFile<TRecord> file)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            var serial = new string(file.Key.CertificateSerialNumber.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
            return $"archive_{serial}_{NameMoment(file.FirstRecordMoment)}_{NameMoment(file.LastRecordMoment)}.zip";
        }

        private static string NameMoment(DateTime moment) => moment.ToString("yyyyMMdd'T'HHmmssfffffff'Z'", CultureInfo.InvariantCulture);

        public async Task WriteAsync(ArchiveFile<TRecord> file, ArchiveMetadata metadata, Stream output, CancellationToken cancellationToken = default)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (metadata is null) throw new ArgumentNullException(nameof(metadata));
            if (output is null) throw new ArgumentNullException(nameof(output));

            // Fail before writing anything when the writer or the signer is missing.
            if (!_writers.TryGetValue(file.Key.FormatVersion, out var writer))
            {
                throw new InvalidOperationException($"No archive file writer for archive format version {file.Key.FormatVersion}.");
            }
            var signer = _signerFor(file.Key) ?? throw new InvalidOperationException($"No archive signer for the SCU certificate {file.Key.CertificateSerialNumber}.");
            if (signer.Certificate is null)
            {
                throw new InvalidOperationException($"The archive signer for the SCU certificate {file.Key.CertificateSerialNumber} has no certificate.");
            }

            var manifest = NewManifest(file, metadata, signer);
            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            var sink = new ZipFileSink(zip, signer, manifest, metadata.GeneratedAt);
            await writer.WriteAsync(file, sink, cancellationToken).ConfigureAwait(false);
            await sink.WriteBytesAsync(ArchiveCertificateFileName, signer.Certificate.RawData, cancellationToken).ConfigureAwait(false);

            var manifestBytes = ManifestSerializer.Serialize(manifest);
            byte[] manifestHash;
            using (var sha256 = SHA256.Create())
            {
                manifestHash = sha256.ComputeHash(manifestBytes);
            }
            var signature = await sink.SignAsync(manifestHash, cancellationToken).ConfigureAwait(false);
            await sink.WriteUnlistedAsync(ManifestFileName, manifestBytes, cancellationToken).ConfigureAwait(false);
            await sink.WriteUnlistedAsync(ManifestSignatureFileName, System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(signature)), cancellationToken).ConfigureAwait(false);
        }

        private ArchiveManifest NewManifest(ArchiveFile<TRecord> file, ArchiveMetadata metadata, IArchiveSigner signer)
        {
            var certificate = signer.Certificate;
            using var sha256 = SHA256.Create();
            return new ArchiveManifest
            {
                Schema = _schemaOf?.Invoke(file),
                CertificateSerialNumber = file.Key.CertificateSerialNumber,
                FormatVersion = file.Key.FormatVersion.Value,
                Siret = metadata.Siret,
                DocumentationUrl = metadata.DocumentationUrl,
                GeneratedAt = metadata.GeneratedAt,
                PeriodStart = file.Period?.Start,
                PeriodEnd = file.Period?.End,
                RequestStart = file.RequestedPeriod?.Start,
                RequestEnd = file.RequestedPeriod?.End,
                Part = file.Part,
                PartCount = file.PartCount,
                FirstRecord = _describe(file.FirstRecord),
                LastRecord = _describe(file.LastRecord),
                RecordCount = file.Records.Count,
                ClosedBy = file.ClosedBy.ToManifestValue(),
                ForcedArchive = file.ForcedArchive,
                SignatureAlgorithm = signer.SignatureAlgorithm,
                SigningCertificate = new ManifestCertificate
                {
                    FileName = ArchiveCertificateFileName,
                    SerialNumber = certificate.SerialNumber,
                    Subject = certificate.Subject,
                    Issuer = certificate.Issuer,
                    Sha256Thumbprint = Convert.ToBase64String(sha256.ComputeHash(certificate.RawData))
                },
                ChainAnchors = _chainAnchors?.Invoke(file).ToList(),
            };
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
                var signature = await SignAsync(hash, cancellationToken).ConfigureAwait(false);
                _manifest.Files.Add(new ManifestFile
                {
                    Name = fileName,
                    Size = size,
                    Hash = Convert.ToBase64String(hash),
                    Signature = Convert.ToBase64String(signature)
                });
            }

            /// <summary>Signs a hash, and checks the signature against the certificate: an archive never carries a signature that does not verify.</summary>
            public async Task<byte[]> SignAsync(byte[] hash, CancellationToken cancellationToken)
            {
                var signature = await _signer.SignHashAsync(hash, cancellationToken).ConfigureAwait(false);
                bool verifies;
                try
                {
                    verifies = signature is not null && signature.Length > 0 && ArchiveSignatureVerifier.Verify(_signer.Certificate, _signer.SignatureAlgorithm, hash, signature);
                }
                catch (CryptographicException)
                {
                    verifies = false;
                }
                if (!verifies)
                {
                    throw new InvalidOperationException($"The archive signer returned a signature that does not verify against its certificate {_signer.Certificate.SerialNumber} ({_signer.SignatureAlgorithm}).");
                }
                return signature;
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
