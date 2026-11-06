using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// A tampered or malformed archive gives an invalid result, never an exception, and ES256 means P-256
    /// (review on fiskaltrust/middleware#823).
    /// </summary>
    public class ArchiveVerifierRobustnessTests
    {
        private static readonly byte[] Hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("manifest"));

        private static async Task<byte[]> Archive()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            return await ArchivePackagerTests.Package(ArchivePackagerTests.TwoSubArchives(), certificate);
        }

        private static string Manifest(byte[] archive) => Encoding.UTF8.GetString(ArchivePackagerTests.ReadEntry(archive, ArchivePackager<TestRecord>.ManifestFileName));

        private static byte[] WithManifest(byte[] archive, Func<string, string> change)
            => ArchivePackagerTests.ReplaceEntry(archive, ArchivePackager<TestRecord>.ManifestFileName, content => Encoding.UTF8.GetBytes(change(Encoding.UTF8.GetString(content))));

        private static X509Certificate2 EcdsaCertificate(ECCurve curve)
        {
            using var ecdsa = ECDsa.Create(curve);
            return new CertificateRequest("CN=archive test", ecdsa, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        [Fact]
        public void An_ecdsa_signer_off_p256_is_refused()
        {
            using var certificate = EcdsaCertificate(ECCurve.NamedCurves.nistP384);

            FluentActions.Invoking(() => new X509ArchiveSigner(certificate)).Should().Throw<ArgumentException>().WithMessage("*P-256*");
        }

        [Fact]
        public void An_es256_signature_from_a_p384_key_does_not_verify()
        {
            using var certificate = EcdsaCertificate(ECCurve.NamedCurves.nistP384);
            using var key = certificate.GetECDsaPrivateKey()!;
            var signature = key.SignHash(Hash);

            ArchiveSignatureVerifier.Verify(certificate, ArchiveSignatureAlgorithms.ES256, Hash, signature).Should().BeFalse();
        }

        [Fact]
        public void An_es256_signature_that_is_not_64_bytes_does_not_verify()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            using var key = certificate.GetECDsaPrivateKey()!;
            var p1363 = key.SignHash(Hash);

            ArchiveSignatureVerifier.Verify(certificate, ArchiveSignatureAlgorithms.ES256, Hash, p1363).Should().BeTrue();
            ArchiveSignatureVerifier.Verify(certificate, ArchiveSignatureAlgorithms.ES256, Hash, Pad(p1363)).Should().BeFalse();
        }

        private static byte[] Pad(byte[] signature)
        {
            var padded = new byte[signature.Length + 1];
            Array.Copy(signature, padded, signature.Length);
            return padded;
        }

        [Fact]
        public async Task A_duplicate_zip_entry_is_a_verification_error()
        {
            var archive = await Archive();
            using var output = new MemoryStream();
            output.Write(archive, 0, archive.Length);
            using (var zip = new ZipArchive(output, ZipArchiveMode.Update, leaveOpen: true))
            {
                using var entry = zip.CreateEntry(ArchivePackager<TestRecord>.ManifestFileName).Open();
                entry.Write(new byte[] { (byte) '{', (byte) '}' }, 0, 2);
            }

            var result = ArchiveVerifier.Verify(new MemoryStream(output.ToArray()));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("more than once"));
        }

        [Theory]
        [InlineData("\"signingCertificate\":", "\"signingCertificate\": null, \"ignored\":")]
        [InlineData("\"files\":", "\"files\": null, \"ignored\":")]
        [InlineData("\"signature\": \"", "\"signature\": null, \"ignored\": \"")]
        public async Task A_manifest_with_nulls_is_a_verification_error(string find, string replace)
        {
            var archive = await Archive();
            Manifest(archive).Should().Contain(find);

            var result = ArchiveVerifier.Verify(new MemoryStream(WithManifest(archive, m => ReplaceFirst(m, find, replace))));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.StartsWith("manifest"));
        }

        private static string ReplaceFirst(string text, string find, string replace)
        {
            var index = text.IndexOf(find, StringComparison.Ordinal);
            return text.Substring(0, index) + replace + text.Substring(index + find.Length);
        }

        [Fact]
        public async Task An_undecodable_certificate_is_a_verification_error()
        {
            var archive = await Archive();

            var tampered = ArchivePackagerTests.ReplaceEntry(archive, ArchivePackager<TestRecord>.ArchiveCertificateFileName, _ => new byte[] { 1, 2, 3 });
            var result = ArchiveVerifier.Verify(new MemoryStream(tampered));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("unreadable"));
        }

        [Fact]
        public async Task A_sub_archive_above_the_in_memory_limit_verifies_through_a_temporary_file()
        {
            var archive = await Archive();

            ArchiveVerifier.Verify(new MemoryStream(archive), new ArchiveVerifier.VerifierLimits { InMemorySubArchiveBytes = 0 }).AllErrors.Should().BeEmpty();
        }

        [Fact]
        public async Task A_sub_archive_that_lists_sub_archives_is_not_followed()
        {
            var archive = await Archive();
            var subName = SubArchiveNames(archive).First();

            // A sub manifest naming one of its own files as a sub-archive would make the verifier recurse.
            var nested = ArchivePackagerTests.ReplaceEntry(archive, subName, sub => ArchivePackagerTests.ReplaceEntry(sub, ArchivePackager<TestRecord>.ManifestFileName,
                content => Encoding.UTF8.GetBytes(ReplaceFirst(Encoding.UTF8.GetString(content), "{", "{\"subArchives\":[{\"fileName\":\"journal-T.csv\"}],"))));
            var result = ArchiveVerifier.Verify(new MemoryStream(nested));

            result.IsValid.Should().BeFalse();
            result.SubArchives.Should().Contain(s => s.Errors.Any(e => e.Contains("nest one level")));
            result.SubArchives.Should().OnlyContain(s => s.SubArchives.Count == 0);
        }

        [Fact]
        public async Task A_zip_with_too_many_entries_is_refused()
        {
            var result = ArchiveVerifier.Verify(new MemoryStream(await Archive()), new ArchiveVerifier.VerifierLimits { MaxEntries = 3 });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("entries"));
        }

        [Fact]
        public async Task The_size_limit_counts_the_bytes_actually_decompressed()
        {
            var result = ArchiveVerifier.Verify(new MemoryStream(await Archive()), new ArchiveVerifier.VerifierLimits { MaxEntryBytes = 100 });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("decompresses to more than 100 bytes"));
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(10u)]
        public async Task A_forged_uncompressed_size_is_a_verification_error(uint declared)
        {
            var archive = await Archive();
            var subName = SubArchiveNames(archive).First();

            // The sub-archive's zip headers claim fewer bytes than it holds. .NET stops reading at the declared size,
            // so memory stays bounded, and the shortened content fails the manifest's size and hash; a runtime that
            // reads on is caught by the declared-size check.
            var forged = ForgeUncompressedSize(archive, subName, declared);
            var verify = () => ArchiveVerifier.Verify(new MemoryStream(forged));

            var result = verify.Should().NotThrow().Subject;
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.StartsWith(subName) && (e.Contains("declares") || e.Contains("cannot be decompressed") || e.Contains("manifest says")));
        }

        private static string[] SubArchiveNames(byte[] archive)
        {
            using var zip = new ZipArchive(new MemoryStream(archive));
            return zip.Entries.Select(e => e.FullName).Where(n => n.StartsWith("subarchive_")).ToArray();
        }

        /// <summary>Rewrites the uncompressed size of one entry in its local header and its central-directory record.</summary>
        private static byte[] ForgeUncompressedSize(byte[] zip, string name, uint size)
        {
            var bytes = (byte[]) zip.Clone();
            var nameBytes = Encoding.UTF8.GetBytes(name);
            for (var i = 0; i + 46 < bytes.Length; i++)
            {
                var signature = BitConverter.ToUInt32(bytes, i);
                var (sizeOffset, nameOffset) = signature switch
                {
                    0x04034b50u => (22, 30), // local file header
                    0x02014b50u => (24, 46), // central directory record
                    _ => (-1, -1),
                };
                if (sizeOffset < 0 || i + nameOffset + nameBytes.Length > bytes.Length)
                {
                    continue;
                }
                var nameLength = BitConverter.ToUInt16(bytes, i + nameOffset - (signature == 0x04034b50u ? 4 : 18));
                if (nameLength == nameBytes.Length && bytes.Skip(i + nameOffset).Take(nameLength).SequenceEqual(nameBytes))
                {
                    BitConverter.GetBytes(size).CopyTo(bytes, i + sizeOffset);
                }
            }
            return bytes;
        }
    }
}
