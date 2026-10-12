using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Packaging;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Signing;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Split;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.UnitTest
{
    public class ArchivePackagerTests
    {
        private const string Siret = "12345678900012";
        private static readonly DateTime GeneratedAt = new DateTime(2026, 10, 12, 9, 30, 0, DateTimeKind.Utc).AddTicks(42);
        private static readonly ArchivePeriod Year2024 = new ArchivePeriod(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        private static MainArchive<TestRecord> TwoSubArchives()
        {
            var records = new[]
            {
                new TestRecord { Moment = new DateTime(2024, 2, 1, 8, 0, 0, DateTimeKind.Utc).AddTicks(7), Serial = "AAAA", Version = ArchiveFormatVersion.V1, Total = 1.5m, Lines = { new TestLine { Description = "x", Amount = 1.5m } } },
                new TestRecord { Moment = new DateTime(2024, 8, 1, 8, 0, 0, DateTimeKind.Utc), Serial = "BBBB", Total = 3m, Text = "é;\"q\"" },
                new TestRecord { Moment = new DateTime(2024, 8, 2, 8, 0, 0, DateTimeKind.Utc), Serial = "BBBB", Chain = "G", Total = 3m }
            };
            return ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, Year2024).Single();
        }

        private static async Task<byte[]> Package(MainArchive<TestRecord> archive, X509Certificate2 certificate)
        {
            var packager = new ArchivePackager<TestRecord>(
                new[] { TestFixtures.Writer(ArchiveFormatVersion.V1), TestFixtures.Writer(ArchiveFormatVersion.V2) },
                new X509ArchiveSigner(certificate));
            using var output = new MemoryStream();
            await packager.WriteAsync(archive, new ArchiveMetadata(Siret, GeneratedAt, "https://docs.example/archive-v2"), output);
            return output.ToArray();
        }

        [Fact]
        public async Task Rsa_signed_archive_round_trips_through_the_verifier()
        {
            using var certificate = TestFixtures.RsaCertificate();

            var bytes = await Package(TwoSubArchives(), certificate);
            var result = ArchiveVerifier.Verify(new MemoryStream(bytes));

            result.AllErrors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            var manifest = result.Manifest!;
            manifest.Kind.Should().Be(ArchiveManifest.MainKind);
            manifest.Siret.Should().Be(Siret);
            manifest.PeriodStart.Should().Be(Year2024.Start);
            manifest.PeriodEnd.Should().Be(Year2024.End);
            manifest.GeneratedAt.Should().Be(GeneratedAt);
            manifest.FirstRecordMoment.Should().Be(new DateTime(2024, 2, 1, 8, 0, 0, DateTimeKind.Utc).AddTicks(7));
            manifest.RecordCount.Should().Be(3);
            manifest.SignatureAlgorithm.Should().Be(ArchiveSignatureAlgorithms.RS256);
            manifest.SigningCertificate.SerialNumber.Should().Be(certificate.SerialNumber);
            manifest.SubArchives!.Select(s => s.FileName).Should().Equal("subarchive_AAAA_v1.zip", "subarchive_BBBB_v2.zip");
            manifest.Files.Select(f => f.Name).Should().Contain(new[] { "subarchive_AAAA_v1.zip", "subarchive_BBBB_v2.zip", ArchivePackager<TestRecord>.ArchiveCertificateFileName });

            result.SubArchives.Should().HaveCount(2);
            var second = result.SubArchives[1].Manifest!;
            second.Kind.Should().Be(ArchiveManifest.SubKind);
            second.CertificateSerialNumber.Should().Be("BBBB");
            second.RecordCount.Should().Be(2);
            second.Files.Select(f => f.Name).Should().Contain(new[] { "journal-T.csv", "journal-G.csv", "charge-items.csv" });
            second.Files.Should().OnlyContain(f => f.Hash.Length > 0 && f.Signature.Length > 0 && f.Size > 0);
        }

        [Fact]
        public async Task Ecdsa_signed_archive_round_trips_through_the_verifier()
        {
            using var certificate = TestFixtures.EcdsaCertificate();

            var result = ArchiveVerifier.Verify(new MemoryStream(await Package(TwoSubArchives(), certificate)));

            result.AllErrors.Should().BeEmpty();
            result.Manifest!.SignatureAlgorithm.Should().Be(ArchiveSignatureAlgorithms.ES256);
        }

        [Fact]
        public async Task Manifest_moments_keep_full_precision()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(TwoSubArchives(), certificate);

            var json = Encoding.UTF8.GetString(ReadEntry(bytes, ArchivePackager<TestRecord>.ManifestFileName));

            json.Should().Contain("\"generatedAt\": \"2026-10-12T09:30:00.0000042Z\"");
            json.Should().Contain("\"firstRecordMoment\": \"2024-02-01T08:00:00.0000007Z\"");
        }

        [Fact]
        public async Task A_tampered_manifest_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(TwoSubArchives(), certificate);

            var tampered = ReplaceEntry(bytes, ArchivePackager<TestRecord>.ManifestFileName,
                content => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(content).Replace(Siret, "99999999900099")));

            var result = ArchiveVerifier.Verify(new MemoryStream(tampered));
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("manifest signature invalid");
        }

        [Fact]
        public async Task A_tampered_csv_inside_a_sub_archive_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(TwoSubArchives(), certificate);

            var tampered = ReplaceEntry(bytes, "subarchive_BBBB_v2.zip",
                sub => ReplaceEntry(sub, "journal-T.csv", csv => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(csv).Replace(";3;", ";4;"))));

            var result = ArchiveVerifier.Verify(new MemoryStream(tampered));
            result.IsValid.Should().BeFalse();
            result.AllErrors.Should().Contain(e => e.Contains("subarchive_BBBB_v2.zip") && e.Contains("hash mismatch"));
        }

        [Fact]
        public async Task An_unlisted_file_is_detected()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var bytes = await Package(TwoSubArchives(), certificate);

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
        public async Task An_empty_year_still_produces_a_signed_archive_with_header_only_files()
        {
            // market-fr#146: an empty period still produces an archive.
            using var certificate = TestFixtures.RsaCertificate();
            var options = new ArchiveSplitOptions { DefaultKey = new SubArchiveKey("CCCC", ArchiveFormatVersion.V2) };
            var empty = ArchiveSplitter.Split(Array.Empty<TestRecord>(), r => r.Moment, r => r.Key, Year2024, options).Single();

            var bytes = await Package(empty, certificate);
            var result = ArchiveVerifier.Verify(new MemoryStream(bytes));

            result.AllErrors.Should().BeEmpty();
            result.Manifest!.RecordCount.Should().Be(0);
            result.Manifest.FirstRecordMoment.Should().BeNull();
            var sub = ReadEntry(bytes, "subarchive_CCCC_v2.zip");
            Encoding.UTF8.GetString(ReadEntry(sub, "journal-T.csv")).Should().Be("ftQueueItemID;ftReceiptMoment;Totalizer;Text\r\n");
        }

        [Fact]
        public async Task An_archive_without_any_sub_archive_is_still_signed()
        {
            using var certificate = TestFixtures.EcdsaCertificate();
            var empty = ArchiveSplitter.Split(Array.Empty<TestRecord>(), r => r.Moment, r => r.Key, Year2024).Single();

            var result = ArchiveVerifier.Verify(new MemoryStream(await Package(empty, certificate)));

            result.AllErrors.Should().BeEmpty();
            result.SubArchives.Should().BeEmpty();
        }

        [Fact]
        public async Task A_missing_writer_for_a_format_version_throws_before_writing()
        {
            using var certificate = TestFixtures.RsaCertificate();
            var packager = new ArchivePackager<TestRecord>(new[] { TestFixtures.Writer(ArchiveFormatVersion.V2) }, new X509ArchiveSigner(certificate));
            using var output = new MemoryStream();

            await FluentActions.Awaiting(() => packager.WriteAsync(TwoSubArchives(), new ArchiveMetadata(Siret, GeneratedAt), output))
                .Should().ThrowAsync<InvalidOperationException>();
            output.Length.Should().Be(0);
        }

        [Fact]
        public void An_invalid_siret_is_rejected()
            => FluentActions.Invoking(() => new ArchiveMetadata("1234", GeneratedAt)).Should().Throw<ArgumentException>();

        private static byte[] ReadEntry(byte[] zipBytes, string name)
        {
            using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            using var entry = zip.GetEntry(name)!.Open();
            using var buffer = new MemoryStream();
            entry.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static byte[] ReplaceEntry(byte[] zipBytes, string name, Func<byte[], byte[]> change)
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
