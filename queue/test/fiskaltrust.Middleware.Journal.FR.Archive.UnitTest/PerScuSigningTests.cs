using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// market-fr#262: each sub-archive is signed with the key of its own SCU, the main manifest is a signed
    /// index signed with the newest sub-archive's SCU, and an export missing one SCU's key fails without output.
    /// </summary>
    public class PerScuSigningTests
    {
        private static readonly ArchiveMetadata Metadata = new ArchiveMetadata("12345678900012", new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc));

        private static ArchivePackager<TestRecord> Packager(Func<SubArchiveKey, IArchiveSigner> signerFor, IArchiveSigner empty)
            => new ArchivePackager<TestRecord>(
                new[] { TestFixtures.Writer(ArchiveFormatVersion.V1), TestFixtures.Writer(ArchiveFormatVersion.V2) },
                signerFor,
                empty,
                sub => sub.Records.GroupBy(r => r.Chain).Select(g => new ManifestChainAnchor { Chain = g.Key, FirstReceiptIdentification = g.Key + "1", PreviousSignature = "" }).ToList(),
                sub => "LNE");

        [Fact]
        public async Task Each_sub_archive_is_signed_by_its_own_SCU_and_the_index_by_the_newest()
        {
            using var older = TestFixtures.EcdsaCertificate();
            using var newer = TestFixtures.EcdsaCertificate();
            var signers = new Dictionary<string, IArchiveSigner> { ["AAAA"] = new X509ArchiveSigner(older), ["BBBB"] = new X509ArchiveSigner(newer) };
            var archive = ArchivePackagerTests.TwoSubArchives();

            using var output = new MemoryStream();
            await Packager(key => signers[key.CertificateSerialNumber], signers["BBBB"]).WriteAsync(archive, Metadata, output);
            var result = ArchiveVerifier.Verify(new MemoryStream(output.ToArray()));

            result.AllErrors.Should().BeEmpty();
            result.Manifest!.SigningCertificate.SerialNumber.Should().Be(newer.SerialNumber, "the index is signed by the newest sub-archive's SCU");
            result.Manifest.Schema.Should().Be("LNE");
            result.SubArchives[0].Manifest!.SigningCertificate.SerialNumber.Should().Be(older.SerialNumber);
            result.SubArchives[1].Manifest!.SigningCertificate.SerialNumber.Should().Be(newer.SerialNumber);
            result.SubArchives[1].Manifest!.ChainAnchors!.Select(a => a.Chain).Should().Equal("T", "Z");

            var index = result.Manifest.SubArchives!;
            index[0].PeriodStart.Should().Be(result.Manifest.PeriodStart);
            index[0].PeriodEnd.Should().Be(index[1].PeriodStart);
            index[1].PeriodEnd.Should().Be(result.Manifest.PeriodEnd);
            index.Should().OnlyContain(s => s.Hash == result.Manifest.Files.Single(f => f.Name == s.FileName).Hash && s.Schema == "LNE");
        }

        [Fact]
        public async Task A_missing_SCU_key_fails_before_anything_is_written()
        {
            using var current = TestFixtures.EcdsaCertificate();
            var signer = new X509ArchiveSigner(current);
            using var output = new MemoryStream();

            var write = () => Packager(key => key.CertificateSerialNumber == "BBBB" ? signer : throw new NotSupportedException("no key for " + key.CertificateSerialNumber), signer)
                .WriteAsync(ArchivePackagerTests.TwoSubArchives(), Metadata, output);

            await write.Should().ThrowAsync<NotSupportedException>().WithMessage("*AAAA*");
            output.Length.Should().Be(0);
        }
    }
}
