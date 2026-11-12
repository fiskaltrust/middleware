using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>Every archive is one standalone signed zip under one certificate, with one manifest kind (ADR 0019, contract section 10).</summary>
    public class ArchivePackagerTests
    {
        private const string Siret = "12345678900012";
        private static readonly DateTime GeneratedAt = new DateTime(2026, 10, 12, 9, 30, 0, DateTimeKind.Utc).AddTicks(42);
        private static readonly ArchivePeriod Year2024 = new ArchivePeriod(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        internal static readonly TestRecord[] Records =
        {
            new TestRecord { Moment = new DateTime(2024, 2, 1, 8, 0, 0, DateTimeKind.Utc).AddTicks(7), Serial = "BBBB", Total = 1.5m, Lines = { new TestLine { Description = "x", Amount = 1.5m } } },
            new TestRecord { Moment = new DateTime(2024, 8, 1, 8, 0, 0, DateTimeKind.Utc), Serial = "BBBB", Total = 3m, Text = "é;\"q\"" },
            new TestRecord { Moment = new DateTime(2024, 8, 2, 8, 0, 0, DateTimeKind.Utc), Serial = "BBBB", Chain = "Z", Total = 3m }
        };

        internal static ArchiveFile<TestRecord> OneFile() => ArchiveSplitter.Split(Records, r => r.Moment, r => r.Key, Year2024).Single();

        internal static ArchivePackager<TestRecord> Packager(IArchiveSigner signer)
            => new ArchivePackager<TestRecord>(
                new[] { TestFixtures.Writer(ArchiveFormatVersion.V1), TestFixtures.Writer(ArchiveFormatVersion.V2) },
                signer,
                r => r.Describe(),
                file => file.Records.GroupBy(r => r.Chain).Select(g => new ManifestChainAnchor { Chain = g.Key, FirstReceiptIdentification = g.First().Describe().ReceiptIdentification }).ToList(),
                file => "LNE");

        internal static async Task<byte[]> Package(ArchiveFile<TestRecord> file, X509Certificate2 certificate)
        {
            using var output = new MemoryStream();
            await Packager(new X509ArchiveSigner(certificate)).WriteAsync(file, new ArchiveMetadata(Siret, GeneratedAt, "https://docs.example/archive-v2"), output);
            return output.ToArray();
        }

        [Fact]
        public async Task An_archive_file_is_one_standalone_signed_zip_with_the_file_manifest()
        {
            using var certificate = TestFixtures.RsaCertificate();

            var bytes = await Package(OneFile(), certificate);
            var result = ArchiveVerifier.Verify(new MemoryStream(bytes));

            result.AllErrors.Should().BeEmpty();
            var manifest = result.Manifest!;
            manifest.Siret.Should().Be(Siret);
            manifest.CertificateSerialNumber.Should().Be("BBBB");
            manifest.FormatVersion.Should().Be("2");
            manifest.Schema.Should().Be("LNE");
            manifest.PeriodStart.Should().Be(Year2024.Start);
            manifest.PeriodEnd.Should().Be(Year2024.End);
            manifest.RequestStart.Should().Be(Year2024.Start);
            manifest.RequestEnd.Should().Be(Year2024.End);
            manifest.Part.Should().Be(1);
            manifest.PartCount.Should().Be(1);
            manifest.ClosedBy.Should().Be("periodEnd");
            manifest.RecordCount.Should().Be(3);
            manifest.FirstRecord!.QueueItemId.Should().Be(Records[0].Id);
            manifest.FirstRecord.ReceiptMoment.Should().Be(Records[0].Moment);
            manifest.LastRecord!.QueueItemId.Should().Be(Records[2].Id);
            manifest.ChainAnchors.Should().HaveCount(2);
            manifest.GeneratedAt.Should().Be(GeneratedAt);
            manifest.SignatureAlgorithm.Should().Be(ArchiveSignatureAlgorithms.RS256);
            manifest.SigningCertificate.SerialNumber.Should().Be(certificate.SerialNumber);
            manifest.Files.Select(f => f.Name).Should().BeEquivalentTo(new[] { "journal-T.csv", "journal-Z.csv", "charge-items.csv", ArchivePackager<TestRecord>.ArchiveCertificateFileName });
            manifest.Files.Should().OnlyContain(f => f.Hash.Length > 0 && f.Signature.Length > 0 && f.Size > 0);
            EntryNames(bytes).Should().NotContain(n => n.EndsWith(".zip") || n.StartsWith("scu_"), "no sub-archive, and archive-certificate.cer is the file's own SCU certificate");
        }

        [Fact]
        public async Task The_manifest_has_no_main_or_sub_archive_keys()
        {
            using var certificate = TestFixtures.EcdsaCertificate();

            var json = Encoding.UTF8.GetString(ReadEntry(await Package(OneFile(), certificate), ArchivePackager<TestRecord>.ManifestFileName));

            json.Should().NotContain("\"kind\"").And.NotContain("\"subArchives\"").And.NotContain("\"run\"").And.NotContain("RecordMoment");
            json.Should().Contain("\"firstRecord\": {").And.Contain("\"ftQueueItemID\"").And.Contain("\"ftReceiptIdentification\"");
            json.Should().Contain("\"manifestVersion\": \"1\"");
        }

        [Fact]
        public async Task Ecdsa_signed_archive_round_trips_through_the_verifier()
        {
            using var certificate = TestFixtures.EcdsaCertificate();

            var result = ArchiveVerifier.Verify(new MemoryStream(await Package(OneFile(), certificate)));

            result.AllErrors.Should().BeEmpty();
            result.Manifest!.SignatureAlgorithm.Should().Be(ArchiveSignatureAlgorithms.ES256);
        }

        [Fact]
        public async Task Manifest_moments_keep_full_precision()
        {
            using var certificate = TestFixtures.RsaCertificate();

            var json = Encoding.UTF8.GetString(ReadEntry(await Package(OneFile(), certificate), ArchivePackager<TestRecord>.ManifestFileName));

            json.Should().Contain("\"generatedAt\": \"2026-10-12T09:30:00.0000042Z\"");
            json.Should().Contain("\"ftReceiptMoment\": \"2024-02-01T08:00:00.0000007Z\"");
        }

        [Fact]
        public void The_file_name_is_the_serial_and_the_first_and_last_record_moments()
            => ArchivePackager<TestRecord>.FileName(OneFile()).Should().Be("archive_BBBB_20240201T0800000000007Z_20240802T0800000000000Z.zip");

        [Fact]
        public async Task A_fixed_file_has_no_slice_or_request_and_names_its_forced_archive()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            var file = ArchiveSplitter.Range(Records, r => r.Id, Records[0].Id, Records[1].Id, r => r.Moment, r => r.Key, forcedArchive: "3f2b6c1e-0000-0000-0000-000000000001");
            using var output = new MemoryStream();

            await Packager(new X509ArchiveSigner(certificate)).WriteAsync(file, new ArchiveMetadata(Siret, GeneratedAt), output);

            var result = ArchiveVerifier.Verify(new MemoryStream(output.ToArray()));
            result.AllErrors.Should().BeEmpty();
            result.Manifest!.PeriodStart.Should().BeNull();
            result.Manifest.RequestStart.Should().BeNull();
            result.Manifest.Part.Should().BeNull();
            result.Manifest.ClosedBy.Should().Be("yearLimit");
            result.Manifest.ForcedArchive.Should().Be("3f2b6c1e-0000-0000-0000-000000000001");
            result.Manifest.LastRecord!.QueueItemId.Should().Be(Records[1].Id);
        }

        [Fact]
        public async Task A_tampered_manifest_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(OneFile(), certificate);

            var tampered = ReplaceEntry(bytes, ArchivePackager<TestRecord>.ManifestFileName,
                content => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(content).Replace(Siret, "99999999900099")));

            var result = ArchiveVerifier.Verify(new MemoryStream(tampered));
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("manifest signature invalid");
        }

        [Fact]
        public async Task A_tampered_csv_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(OneFile(), certificate);

            var tampered = ReplaceEntry(bytes, "journal-T.csv", csv => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(csv).Replace(";3;", ";4;")));

            var result = ArchiveVerifier.Verify(new MemoryStream(tampered));
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("journal-T.csv hash mismatch");
        }

        [Fact]
        public async Task An_unlisted_file_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(OneFile(), certificate);

            byte[] withExtra;
            using (var buffer = new MemoryStream())
            {
                buffer.Write(bytes, 0, bytes.Length);
                using (var zip = new ZipArchive(buffer, ZipArchiveMode.Update, leaveOpen: true))
                using (var entry = zip.CreateEntry("extra.csv").Open())
                {
                    entry.WriteByte(1);
                }
                withExtra = buffer.ToArray();
            }

            ArchiveVerifier.Verify(new MemoryStream(withExtra)).Errors.Should().Contain("extra.csv is not listed in the manifest");
        }

        [Fact]
        public async Task A_slice_longer_than_a_year_is_a_verification_error()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            var bytes = await Package(OneFile(), certificate);
            var json = Encoding.UTF8.GetString(ReadEntry(bytes, ArchivePackager<TestRecord>.ManifestFileName));

            var resigned = await Resign(json.Replace("\"periodEnd\": \"2025-01-01T00:00:00Z\"", "\"periodEnd\": \"2025-03-01T00:00:00Z\""), bytes, certificate);

            ArchiveVerifier.Verify(new MemoryStream(resigned)).Errors.Should().Contain("manifest slice is longer than a year");
        }

        [Fact]
        public async Task A_missing_writer_for_the_format_version_throws_before_writing()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var packager = new ArchivePackager<TestRecord>(new[] { TestFixtures.Writer(ArchiveFormatVersion.V1) }, new X509ArchiveSigner(certificate), r => r.Describe());
            using var output = new MemoryStream();

            await FluentActions.Awaiting(() => packager.WriteAsync(OneFile(), new ArchiveMetadata(Siret, GeneratedAt), output))
                .Should().ThrowAsync<InvalidOperationException>();
            output.Length.Should().Be(0);
        }

        [Fact]
        public async Task A_signer_whose_signature_does_not_verify_fails_the_file()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            var packager = Packager(new BrokenSigner(new X509ArchiveSigner(certificate)));
            using var output = new MemoryStream();

            await FluentActions.Awaiting(() => packager.WriteAsync(OneFile(), new ArchiveMetadata(Siret, GeneratedAt), output))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("*does not verify*");
        }

        [Fact]
        public void An_invalid_siret_is_rejected()
            => FluentActions.Invoking(() => new ArchiveMetadata("1234", GeneratedAt)).Should().Throw<ArgumentException>().WithMessage("*1234*");

        private sealed class BrokenSigner : IArchiveSigner
        {
            private readonly IArchiveSigner _inner;

            public BrokenSigner(IArchiveSigner inner) => _inner = inner;

            public X509Certificate2 Certificate => _inner.Certificate;

            public string SignatureAlgorithm => _inner.SignatureAlgorithm;

            public async Task<byte[]> SignHashAsync(byte[] sha256, CancellationToken cancellationToken)
            {
                var signature = await _inner.SignHashAsync(sha256, cancellationToken);
                signature[0] ^= 0xFF;
                return signature;
            }
        }

        /// <summary>The archive with <paramref name="manifestJson"/> as its manifest, signed again with the archive's key.</summary>
        private static async Task<byte[]> Resign(string manifestJson, byte[] archive, X509Certificate2 certificate)
        {
            var bytes = Encoding.UTF8.GetBytes(manifestJson);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var signature = await new X509ArchiveSigner(certificate).SignHashAsync(sha256.ComputeHash(bytes), CancellationToken.None);
            var withManifest = ReplaceEntry(archive, ArchivePackager<TestRecord>.ManifestFileName, _ => bytes);
            return ReplaceEntry(withManifest, ArchivePackager<TestRecord>.ManifestSignatureFileName, _ => Encoding.ASCII.GetBytes(Convert.ToBase64String(signature)));
        }

        internal static string[] EntryNames(byte[] zipBytes)
        {
            using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            return zip.Entries.Select(e => e.FullName).ToArray();
        }

        internal static byte[] ReadEntry(byte[] zipBytes, string name)
        {
            using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            using var entry = zip.GetEntry(name)!.Open();
            using var buffer = new MemoryStream();
            entry.CopyTo(buffer);
            return buffer.ToArray();
        }

        internal static byte[] ReplaceEntry(byte[] zipBytes, string name, Func<byte[], byte[]> change)
        {
            using var source = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            using var output = new MemoryStream();
            using (var target = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in source.Entries)
                {
                    byte[] content;
                    using (var input = entry.Open())
                    using (var buffer = new MemoryStream())
                    {
                        input.CopyTo(buffer);
                        content = buffer.ToArray();
                    }
                    if (entry.FullName == name)
                    {
                        content = change(content);
                    }
                    using var write = target.CreateEntry(entry.FullName).Open();
                    write.Write(content, 0, content.Length);
                }
            }
            return output.ToArray();
        }
    }
}
