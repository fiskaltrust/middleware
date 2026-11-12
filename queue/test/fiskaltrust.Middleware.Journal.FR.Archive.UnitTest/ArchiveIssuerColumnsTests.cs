using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;
using static fiskaltrust.Middleware.Journal.FR.Archive.UnitTest.ArchiveV2FilesTests;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// The issuer columns (archive-v2 §1.1, market-fr#221, decisions 32 and 35) copy the issuer item the SCU printed
    /// (signature type 0x0013, not signed) on the documents that carry the issuer in their header: chains T, B, P and I.
    /// </summary>
    public class ArchiveIssuerColumnsTests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        private const string IssuerData = "SIRET : 12345678901234\\nSIREN : 123456789\\nDénomination : Café de la Gare\\nAdresse : 1 place de la Gare\\nRCS : RCS Paris B 123456789\\nCapital social : 10000 EUR\\nOption TVA : receipts";

        private static string IssuerItem() =>
            ",{\"ftSignatureFormat\":65537,\"ftSignatureType\":" + 0x4652_2000_0000_0013L + ",\"Caption\":\"Émetteur\",\"Data\":\"" + IssuerData + "\"}";

        private static object? Column(ArchiveEntry entry, string name) => ArchiveV2Files.CommonColumns.Single(x => x.Name == name).Value(entry);

        private static ArchiveEntry Read(string identification, long receiptCase, string extraItems)
            => ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, Request(receiptCase), Response(identification, "t1", extraItems: extraItems))!;

        [Theory]
        [InlineData("ft1#T1", 0x4652_2000_0000_0001)]
        [InlineData("ft1#B1", 0x4652_2000_0000_0006)]
        [InlineData("ft1#P1", 0x4652_2000_0000_0002)]
        [InlineData("ft1#I1", 0x4652_2000_0000_1001)]
        public void Documents_CopyTheIssuerItem(string identification, long receiptCase)
        {
            var entry = Read(identification, receiptCase, IssuerItem());

            Column(entry, "Siren").Should().Be("123456789");
            Column(entry, "IssuerName").Should().Be("Café de la Gare");
            Column(entry, "IssuerAddress").Should().Be("1 place de la Gare");
            Column(entry, "Rcs").Should().Be("RCS Paris B 123456789");
            Column(entry, "ShareCapital").Should().Be("10000 EUR");
            Column(entry, "VatOption").Should().Be("receipts");
            Column(entry, "IssuerCity").Should().BeNull("not configured, so no line");
        }

        [Fact]
        public void OtherChains_HaveNoIssuerColumns()
        {
            var entry = Read("ft1#Z1", 0x4652_2000_0000_2011, IssuerItem());

            foreach (var name in new[] { "Siren", "IssuerName", "Rcs", "Naf", "LegalForm", "ShareCapital", "VatOption" })
            {
                Column(entry, name).Should().BeNull(name);
            }
        }

        [Fact]
        public void WithoutAnIssuerItem_TheSignedClaimsOfADataSetStillFillTheColumns()
        {
            var id = Guid.NewGuid();
            var dataSet = ArchiveEntrySignedLinksTests.LneDataSet(id, "ft1#T1", 1, null);
            var entry = ArchiveEntry.Read(id, QueueId, 1, Request(), Response("ft1#T1", dataSet + ".t1"))!;

            Column(entry, "Rcs").Should().Be("RCS Paris", "an LNE SCU prints no issuer item yet: its data set signs the claim");
            Column(entry, "ShareCapital").Should().BeNull();
        }
    }
}
