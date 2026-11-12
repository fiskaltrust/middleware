using System.Linq;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// README.txt is readable without the software (archive-v2 §11.1, [06 D12, D13], [07 §6.9.2.5, §6.9.3.1], ADR 0018):
    /// complete value lists, the canonical request form, the file cut at the one-year limit, the flags a POS acts on,
    /// keeping archives on external media and the closing statement.
    /// </summary>
    public class ArchiveReadmeContentTests
    {
        private static string Readme() => ArchiveV2Files.Files().OfType<ArchiveReadmeFile>().Single().Build("\n");

        [Theory]
        [InlineData("0x0006", "note")]
        [InlineData("0x0007", "pro forma")]
        [InlineData("0x2001", "réservé")]
        [InlineData("0x3011", "reçu d'archive")]
        [InlineData("0x4011", "changement de module de signature")]
        [InlineData("0x4012", "changement de module de signature")]
        [InlineData("0x4021", "migration")]
        [InlineData("0x4022", "migration")]
        public void TheReceiptCases_AreComplete(string code, string label)
            => Readme().Split('\n').Should().Contain(l => l.Contains(code + " :") && l.Contains(label));

        [Fact]
        public void TheReceiptFlags_IncludeTheGroupingOfCompositeArticles()
            => Readme().Should().Contain("0x8000000 : regroupement par numéro de position");

        [Theory]
        [InlineData("0x0006")]
        [InlineData("0x000A")]
        [InlineData("0x000C")]
        [InlineData("0x000D")]
        public void TheSignatureTypes_AreComplete(string code)
            => Readme().Split('\n').Should().Contain(l => l.TrimStart().StartsWith(code + " :"));

        [Fact]
        public void TheSignatureFormats_AreListed()
        {
            var readme = Readme();

            readme.Should().Contain("ftSignatureFormat");
            readme.Should().Contain("0x01 : texte").And.Contain("0x03 : QR code").And.Contain("0x0D : Base64");
        }

        [Fact]
        public void TheCodedColumnsOfTheJournals_HaveTheirValueLists()
            => Readme().Should().Contain("receipt-without-obligation").And.Contain("refund").And.Contain("lifecycle").And.Contain("pro-forma");

        [Fact]
        public void TheStateFlagsAPosActsOn_AreExplained()
            => Readme().Should().Contain("0x0040").And.Contain("0x0800").And.Contain("ArchiveIsDue").And.Contain("MessageIsPending");

        [Fact]
        public void TheCanonicalRequestForm_IsStated()
            => Readme().Should().Contain("version 1").And.Contain("SHA-256").And.Contain("ordre croissant").And.Contain("sans exposant");

        [Fact]
        public void TheFileCutAtTheOneYearLimit_IsExplained()
            => Readme().Should().Contain("ForcedArchive").And.Contain("dernière clôture journalière");

        [Fact]
        public void KeepingArchivesOnExternalMedia_IsRequired()
            => Readme().Should().Contain("support externe");

        [Fact]
        public void TheClosingStatement_IsMade()
            => Readme().Should().Contain("dispense de clôture");
    }
}
