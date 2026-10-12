using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Signing;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Packaging
{
    public sealed class ArchiveVerificationResult
    {
        internal ArchiveVerificationResult(string path) => Path = path;

        /// <summary>"/" for the main archive, the sub-archive file name otherwise.</summary>
        public string Path { get; }

        public ArchiveManifest? Manifest { get; internal set; }

        /// <summary>
        /// The certificate the archive was signed with. The verifier only checks the signatures against it;
        /// checking that it chains to a pinned fiskaltrust root is the caller's job (market-fr#150).
        /// </summary>
        public X509Certificate2? SigningCertificate { get; internal set; }

        public List<string> Errors { get; } = new List<string>();

        public List<ArchiveVerificationResult> SubArchives { get; } = new List<ArchiveVerificationResult>();

        public bool IsValid => Errors.Count == 0 && SubArchives.All(s => s.IsValid);

        public IEnumerable<string> AllErrors => Errors.Select(e => $"{Path}: {e}").Concat(SubArchives.SelectMany(s => s.AllErrors));
    }

    /// <summary>
    /// Checks an archive written by <see cref="ArchivePackager{TRecord}"/>: the manifest signature, every
    /// file's hash and signature, that no file is unlisted, and the same for each sub-archive.
    /// </summary>
    public static class ArchiveVerifier
    {
        public static ArchiveVerificationResult Verify(Stream archive)
        {
            if (archive is null) throw new ArgumentNullException(nameof(archive));
            return Verify(archive, "/", ArchiveManifest.MainKind);
        }

        private static ArchiveVerificationResult Verify(Stream archive, string path, string expectedKind)
        {
            var result = new ArchiveVerificationResult(path);
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
                var entries = zip.Entries.ToDictionary(e => e.FullName, StringComparer.Ordinal);
                if (!entries.TryGetValue(ArchivePackager<object>.ManifestFileName, out var manifestEntry)
                    || !entries.TryGetValue(ArchivePackager<object>.ManifestSignatureFileName, out var signatureEntry))
                {
                    result.Errors.Add("manifest.json or manifest.sig missing");
                    return result;
                }

                var manifestBytes = ReadAll(manifestEntry);
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
                result.Manifest = manifest;
                if (!string.Equals(manifest.Kind, expectedKind, StringComparison.Ordinal))
                {
                    result.Errors.Add($"manifest kind is '{manifest.Kind}', expected '{expectedKind}'");
                }

                if (!entries.TryGetValue(manifest.SigningCertificate.FileName, out var certificateEntry))
                {
                    result.Errors.Add($"signing certificate {manifest.SigningCertificate.FileName} missing");
                    return result;
                }
                var certificateBytes = ReadAll(certificateEntry);
                var certificate = new X509Certificate2(certificateBytes);
                result.SigningCertificate = certificate;
                if (Convert.ToBase64String(Sha256(certificateBytes)) != manifest.SigningCertificate.Sha256Thumbprint)
                {
                    result.Errors.Add("signing certificate does not match the manifest thumbprint");
                }

                if (!Verify(certificate, manifest.SignatureAlgorithm, Sha256(manifestBytes), ReadBase64(signatureEntry)))
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
                    var content = ReadAll(entry);
                    var hash = Sha256(content);
                    if (content.LongLength != file.Size)
                    {
                        result.Errors.Add($"{file.Name} size {content.LongLength}, manifest says {file.Size}");
                    }
                    if (Convert.ToBase64String(hash) != file.Hash)
                    {
                        result.Errors.Add($"{file.Name} hash mismatch");
                    }
                    if (!Verify(certificate, manifest.SignatureAlgorithm, hash, FromBase64(file.Signature)))
                    {
                        result.Errors.Add($"{file.Name} signature invalid");
                    }
                }
                foreach (var unlisted in entries.Keys.Where(k => !listed.Contains(k)))
                {
                    result.Errors.Add($"{unlisted} is not listed in the manifest");
                }

                foreach (var subArchive in manifest.SubArchives ?? new List<ManifestSubArchive>())
                {
                    if (!manifest.Files.Any(f => f.Name == subArchive.FileName) || !entries.TryGetValue(subArchive.FileName, out var entry))
                    {
                        result.Errors.Add($"sub-archive {subArchive.FileName} is not a listed file");
                        continue;
                    }
                    using var buffer = new MemoryStream(ReadAll(entry));
                    var sub = Verify(buffer, subArchive.FileName, ArchiveManifest.SubKind);
                    if (sub.Manifest is not null
                        && (sub.Manifest.CertificateSerialNumber != subArchive.CertificateSerialNumber || sub.Manifest.FormatVersion != subArchive.FormatVersion
                            || sub.Manifest.PeriodStart != manifest.PeriodStart || sub.Manifest.PeriodEnd != manifest.PeriodEnd || sub.Manifest.Siret != manifest.Siret))
                    {
                        sub.Errors.Add("sub-archive manifest does not match the main manifest");
                    }
                    result.SubArchives.Add(sub);
                }
            }
            return result;
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

        private static byte[] ReadAll(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static byte[]? ReadBase64(ZipArchiveEntry entry) => FromBase64(Encoding.ASCII.GetString(ReadAll(entry)).Trim());

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
