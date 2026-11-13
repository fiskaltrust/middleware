using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;

namespace fiskaltrust.Middleware.Journal.FR.Archive.Packaging
{
    public sealed class ArchiveVerificationResult
    {
        internal ArchiveVerificationResult(string path) => Path = path;

        /// <summary>"/": the archive file itself (ADR 0019: one standalone file, no sub-archive).</summary>
        public string Path { get; }

        public ArchiveManifest? Manifest { get; internal set; }

        /// <summary>
        /// The certificate the archive was signed with. The verifier only checks the signatures against it;
        /// checking that it chains to a pinned fiskaltrust root is the caller's job (market-fr#150).
        /// </summary>
        public X509Certificate2? SigningCertificate { get; internal set; }

        public List<string> Errors { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        public IEnumerable<string> AllErrors => Errors.Select(e => $"{Path}: {e}");
    }

    /// <summary>
    /// Checks an archive file written by <see cref="ArchivePackager{TRecord}"/> (ADR 0019): the manifest signature, every
    /// file's hash and signature, that no file is unlisted, and the manifest's own coherence (a slice of at most a year,
    /// its part among the request's files, its first and last record). A tampered or malformed archive is reported in
    /// <see cref="ArchiveVerificationResult.Errors"/>, never thrown. The receipts, their chains and the certificate's
    /// trust are TaxAuditorTool's checks (contract section 10.5).
    /// </summary>
    /// <remarks>
    /// Files are hashed from their streams, so an annual archive does not have to fit in memory. A crafted zip is
    /// refused, not followed: every size is counted on the bytes actually decompressed and must match the declared one,
    /// and a zip holds at most <see cref="DefaultMaxEntries"/> entries.
    /// </remarks>
    public static class ArchiveVerifier
    {

        /// <summary>manifest.json, manifest.sig and the certificate are read whole; nothing legitimate comes near this.</summary>
        private const long SmallFileLimit = 1024 * 1024;

        /// <summary>
        /// The most entries one zip may hold. An archive file holds about twenty files, so this only stops a crafted zip.
        /// </summary>
        public const int DefaultMaxEntries = 4096;

        /// <summary>The largest decompressed entry, counted on the bytes actually read, not on the zip's declared size.</summary>
        public const long DefaultMaxEntryBytes = 8L * 1024 * 1024 * 1024;


        public static ArchiveVerificationResult Verify(Stream archive)
        {
            if (archive is null) throw new ArgumentNullException(nameof(archive));
            return Verify(archive, VerifierLimits.Default);
        }

        /// <summary>With lowered limits, so tests reach them without huge archives.</summary>
        internal static ArchiveVerificationResult Verify(Stream archive, VerifierLimits limits)
        {
            var result = new ArchiveVerificationResult("/");
            ZipArchive zip;
            try
            {
                zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
            }
            catch (InvalidDataException e)
            {
                result.Errors.Add($"not a zip archive ({e.Message})");
                return result;
            }

            using (zip)
            {
                if (zip.Entries.Count > limits.MaxEntries)
                {
                    result.Errors.Add($"the zip holds {zip.Entries.Count} entries, more than the {limits.MaxEntries} an archive may hold");
                    return result;
                }
                var duplicates = zip.Entries.GroupBy(e => e.FullName, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicates.Count > 0)
                {
                    result.Errors.AddRange(duplicates.Select(name => $"{name} appears more than once in the zip"));
                    return result;
                }
                var entries = zip.Entries.ToDictionary(e => e.FullName, StringComparer.Ordinal);
                if (!entries.TryGetValue(ArchivePackager<object>.ManifestFileName, out var manifestEntry)
                    || !entries.TryGetValue(ArchivePackager<object>.ManifestSignatureFileName, out var signatureEntry))
                {
                    result.Errors.Add("manifest.json or manifest.sig missing");
                    return result;
                }

                if (!TryReadSmall(manifestEntry, result, out var manifestBytes))
                {
                    return result;
                }
                ArchiveManifest manifest;
                try
                {
                    manifest = ManifestSerializer.Deserialize(manifestBytes);
                }
                catch (Exception e) when (e is System.Text.Json.JsonException || e is NotSupportedException)
                {
                    result.Errors.Add($"manifest.json unreadable ({e.Message})");
                    return result;
                }
                var structureErrors = StructureErrors(manifest).ToList();
                if (structureErrors.Count > 0)
                {
                    result.Errors.AddRange(structureErrors);
                    return result;
                }
                result.Manifest = manifest;
                result.Errors.AddRange(CoherenceErrors(manifest));

                if (!entries.TryGetValue(manifest.SigningCertificate.FileName, out var certificateEntry))
                {
                    result.Errors.Add($"signing certificate {manifest.SigningCertificate.FileName} missing");
                    return result;
                }
                if (!TryReadSmall(certificateEntry, result, out var certificateBytes))
                {
                    return result;
                }
                X509Certificate2 certificate;
                try
                {
                    certificate = new X509Certificate2(certificateBytes);
                }
                catch (CryptographicException e)
                {
                    result.Errors.Add($"signing certificate {manifest.SigningCertificate.FileName} unreadable ({e.Message})");
                    return result;
                }
                result.SigningCertificate = certificate;
                if (Convert.ToBase64String(Sha256(certificateBytes)) != manifest.SigningCertificate.Sha256Thumbprint)
                {
                    result.Errors.Add("signing certificate does not match the manifest thumbprint");
                }

                if (!TryReadSmall(signatureEntry, result, out var signatureBytes))
                {
                    return result;
                }
                if (!Verify(certificate, manifest.SignatureAlgorithm, Sha256(manifestBytes), FromBase64(Encoding.ASCII.GetString(signatureBytes).Trim())))
                {
                    result.Errors.Add("manifest signature invalid");
                }

                var listed = new HashSet<string>(StringComparer.Ordinal) { ArchivePackager<object>.ManifestFileName, ArchivePackager<object>.ManifestSignatureFileName };
                foreach (var file in manifest.Files)
                {
                    listed.Add(file.Name);
                    if (!entries.TryGetValue(file.Name, out var entry))
                    {
                        result.Errors.Add($"{file.Name} listed but missing");
                        continue;
                    }
                    var (hash, size, unreadable) = HashEntry(entry, limits);
                    if (unreadable is not null)
                    {
                        result.Errors.Add(unreadable);
                        continue;
                    }
                    if (size != file.Size)
                    {
                        result.Errors.Add($"{file.Name} size {size}, manifest says {file.Size}");
                    }
                    if (Convert.ToBase64String(hash) != file.Hash)
                    {
                        result.Errors.Add($"{file.Name} hash mismatch");
                    }
                    if (!Verify(certificate, manifest.SignatureAlgorithm, hash, FromBase64(file.Signature)))
                    {
                        result.Errors.Add($"{file.Name} signature invalid");
                    }
                    if (file.Name == RequestsFile.Name)
                    {
                        using var content = entry.Open();
                        if (RequestsFile.Validate(content) is { } problem)
                        {
                            result.Errors.Add($"{file.Name}: {problem}");
                        }
                    }
                }
                foreach (var unlisted in entries.Keys.Where(k => !listed.Contains(k)))
                {
                    result.Errors.Add($"{unlisted} is not listed in the manifest");
                }

            }
            return result;
        }

        /// <summary>
        /// What the verifier relies on. Nullable annotations do not stop a tampered manifest from carrying
        /// <c>null</c>, so each value is checked before it is used.
        /// </summary>
        private static IEnumerable<string> StructureErrors(ArchiveManifest manifest)
        {
            if (manifest.SigningCertificate is null)
            {
                yield return "manifest has no signingCertificate";
            }
            else
            {
                if (string.IsNullOrEmpty(manifest.SigningCertificate.FileName)) yield return "manifest signingCertificate has no fileName";
                if (manifest.SigningCertificate.Sha256Thumbprint is null) yield return "manifest signingCertificate has no sha256Thumbprint";
            }
            if (manifest.SignatureAlgorithm is null) yield return "manifest has no signatureAlgorithm";
            if (manifest.Files is null)
            {
                yield return "manifest has no files";
            }
            else
            {
                for (var i = 0; i < manifest.Files.Count; i++)
                {
                    var file = manifest.Files[i];
                    if (file is null || string.IsNullOrEmpty(file.Name) || file.Hash is null || file.Signature is null)
                    {
                        yield return $"manifest file {i} lacks its name, hash or signature";
                    }
                }
            }
        }

        /// <summary>What the manifest says of the file itself (contract section 10.2, 10.5).</summary>
        private static IEnumerable<string> CoherenceErrors(ArchiveManifest manifest)
        {
            if (manifest.PeriodStart is { } start && manifest.PeriodEnd is { } end)
            {
                if (end <= start)
                {
                    yield return "manifest periodEnd is not after periodStart";
                }
                else if (end > start.AddYears(1))
                {
                    yield return "manifest slice is longer than a year";
                }
            }
            else if (manifest.PeriodStart is not null || manifest.PeriodEnd is not null)
            {
                yield return "manifest has only one of periodStart and periodEnd";
            }
            if (manifest.Part is { } part && (manifest.PartCount is not { } count || part < 1 || part > count))
            {
                yield return "manifest part is not within partCount";
            }
            if (manifest.RecordCount < 1 || manifest.FirstRecord is null || manifest.LastRecord is null)
            {
                yield return "manifest names no first and last record; an archive file holds at least one";
            }
            if (string.IsNullOrEmpty(manifest.ClosedBy))
            {
                yield return "manifest has no closedBy";
            }
        }

        private static bool Verify(X509Certificate2 certificate, string algorithm, byte[] hash, byte[]? signature)
        {
            try
            {
                return signature is not null && ArchiveSignatureVerifier.Verify(certificate, algorithm, hash, signature);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static bool TryReadSmall(ZipArchiveEntry entry, ArchiveVerificationResult result, out byte[] content)
        {
            content = Array.Empty<byte>();
            if (entry.Length > SmallFileLimit)
            {
                result.Errors.Add($"{entry.FullName} is {entry.Length} bytes, more than a manifest, signature or certificate holds");
                return false;
            }
            using var buffer = new MemoryStream();
            var problem = Decompress(entry, SmallFileLimit, (bytes, count) => buffer.Write(bytes, 0, count), out _);
            if (problem is not null)
            {
                result.Errors.Add(problem);
                return false;
            }
            content = buffer.ToArray();
            return true;
        }

        private static (byte[] Hash, long Size, string? Problem) HashEntry(ZipArchiveEntry entry, VerifierLimits limits)
        {
            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var problem = Decompress(entry, limits.MaxEntryBytes, (bytes, count) => sha256.AppendData(bytes, 0, count), out var size);
            return (sha256.GetHashAndReset(), size, problem);
        }

        /// <summary>
        /// Streams an entry's decompressed bytes into <paramref name="sink"/>, counting what is actually read.
        /// Returns why the entry is unusable: it decompresses past <paramref name="limit"/>, its data is corrupt,
        /// or it holds another size than the zip declares (forged metadata). Null when it is fine.
        /// </summary>
        private static string? Decompress(ZipArchiveEntry entry, long limit, Action<byte[], int> sink, out long size)
        {
            size = 0;
            try
            {
                using var stream = entry.Open();
                var buffer = new byte[81920];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    size += read;
                    if (size > limit)
                    {
                        return $"{entry.FullName} decompresses to more than {limit} bytes";
                    }
                    sink(buffer, read);
                }
            }
            catch (InvalidDataException e)
            {
                return $"{entry.FullName} cannot be decompressed ({e.Message})";
            }
            return size == entry.Length ? null : $"{entry.FullName} declares {entry.Length} bytes but holds {size}";
        }

        internal sealed class VerifierLimits
        {
            public static readonly VerifierLimits Default = new VerifierLimits();

            public int MaxEntries { get; set; } = DefaultMaxEntries;

            public long MaxEntryBytes { get; set; } = DefaultMaxEntryBytes;
        }

        private static byte[]? FromBase64(string value)
        {
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static byte[] Sha256(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }
    }
}
