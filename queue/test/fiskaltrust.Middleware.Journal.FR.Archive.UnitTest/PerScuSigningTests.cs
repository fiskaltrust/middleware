using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// market-fr#262, ADR 0019: each archive file holds one certificate's entries and is signed with that SCU's key,
    /// found by the certificate serial of its entries; a file whose key is missing fails without output.
    /// </summary>
    public class PerScuSigningTests
    {
        private static readonly ArchiveMetadata Metadata = new ArchiveMetadata("12345678900012", new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc));
        private static readonly ArchivePeriod Year2026 = new ArchivePeriod(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        private static ArchivePackager<TestRecord> Packager(Func<ArchiveFileKey, IArchiveSigner> signerFor)
            => new ArchivePackager<TestRecord>(new[] { TestFixtures.Writer(ArchiveFormatVersion.V2) }, signerFor, r => r.Describe(), schemaOf: _ => "LNE");

        [Fact]
        public async Task Each_file_is_signed_by_the_SCU_of_its_entries()
        {
            using var older = TestFixtures.EcdsaCertificate();
            using var newer = TestFixtures.EcdsaCertificate();
            var signers = new Dictionary<string, IArchiveSigner> { ["AAAA"] = new X509ArchiveSigner(older), ["BBBB"] = new X509ArchiveSigner(newer) };
            var files = ArchiveSplitter.Split(new[] { TestRecord.At(2026, 2, 1, "AAAA"), TestRecord.At(2026, 8, 1, "BBBB") }, r => r.Moment, r => r.Key, Year2026);

            var results = new List<ArchiveVerificationResult>();
            foreach (var file in files)
            {
                using var output = new MemoryStream();
                await Packager(key => signers[key.CertificateSerialNumber]).WriteAsync(file, Metadata, output);
                results.Add(ArchiveVerifier.Verify(new MemoryStream(output.ToArray())));
            }

            results.Should().OnlyContain(r => r.IsValid);
            results.Select(r => r.Manifest!.CertificateSerialNumber).Should().Equal("AAAA", "BBBB");
            results.Select(r => r.SigningCertificate!.Thumbprint).Should().Equal(older.Thumbprint, newer.Thumbprint);
            results[0].Manifest!.ClosedBy.Should().Be("lastSignedEntry");
            results.Select(r => r.Manifest!.Part).Should().Equal(1, 2);
        }

        [Fact]
        public async Task A_file_whose_SCU_key_is_missing_fails_before_anything_is_written()
        {
            var file = ArchiveSplitter.Split(new[] { TestRecord.At(2026, 2, 1, "AAAA") }, r => r.Moment, r => r.Key, Year2026).Single();
            using var output = new MemoryStream();

            await FluentActions.Awaiting(() => Packager(key => throw new NotSupportedException($"no key for {key.CertificateSerialNumber}")).WriteAsync(file, Metadata, output))
                .Should().ThrowAsync<NotSupportedException>().WithMessage("*AAAA*");
            output.Length.Should().Be(0);
        }
    }
}
