using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class JournalTypeFRTests
{
    // The codes of the v2 archive contract (market-fr doc/nf525/contracts/archive-v2.md, section 7).
    [Theory]
    [InlineData(0x000, JournalTypeFR.StatusInformation)]
    [InlineData(0x001, JournalTypeFR.Ticket)]
    [InlineData(0x002, JournalTypeFR.PaymentProof)]
    [InlineData(0x003, JournalTypeFR.Invoice)]
    [InlineData(0x004, JournalTypeFR.GrandTotal)]
    [InlineData(0x007, JournalTypeFR.Bill)]
    [InlineData(0x008, JournalTypeFR.Archive)]
    [InlineData(0x009, JournalTypeFR.Log)]
    [InlineData(0x00A, JournalTypeFR.Copy)]
    [InlineData(0x00B, JournalTypeFR.Training)]
    [InlineData(0x010, JournalTypeFR.ArchiveExport)]
    public void JournalTypeFR_FollowsTheArchiveContractCodes(long code, JournalTypeFR expected)
    {
        ((long) expected).Should().Be(0x4652_2000_0000_0000 | code);
    }

    [Fact]
    public void JournalTypeFR_ListsElevenJournals()
    {
        Enum.GetValues<JournalTypeFR>().Should().HaveCount(11).And.OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(0x4652_2000_0000_0007)] // v2-shaped request
    [InlineData(0x4652_0000_0000_0007)] // v1-shaped request (no version nibble)
    [InlineData(0x4652_2000_0001_0007)] // with a gggg flag set
    public void TypeFR_MatchesOnTheJournalCodeOnly(long ftJournalType)
    {
        var journalType = (JournalType) ftJournalType;

        journalType.TypeFR().Should().Be(JournalTypeFR.Bill);
        journalType.IsType(JournalTypeFR.Bill).Should().BeTrue();
        journalType.IsType(JournalTypeFR.Log).Should().BeFalse();
    }

    [Fact]
    public void JournalProcessorFR_RefusesTheArchiveJournalUntilArchiveReceiptsExist()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = JournalTypeFR.Archive.As<JournalType>() });

        act.Should().Throw<NotSupportedException>().WithMessage("*does not sign archive receipts yet*");
    }

    [Fact]
    public void JournalProcessorFR_RefusesTheArchiveExportWithoutAnArchiveSigner()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = JournalTypeFR.ArchiveExport.As<JournalType>() });

        act.Should().Throw<NotSupportedException>().WithMessage("*needs an archive signer*");
    }

    [Fact]
    public async Task ScuArchiveSigner_RefusesAnScuThatDoesNotSignArchives()
    {
        // FakeFRSSCD keeps the contract's default for GetSigningCertificateAsync, as an SCU without archive signing does.
        var act = () => ScuArchiveSigner.CreateAsync(new FakeFRSSCD());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*SCU cannot sign the archive export*");
    }

    [Theory]
    [InlineData(JournalTypeFR.Ticket, "text/csv")]
    [InlineData(JournalTypeFR.GrandTotal, "text/csv")]
    [InlineData(JournalTypeFR.Training, "text/csv")]
    [InlineData(JournalTypeFR.StatusInformation, "application/json")]
    public void JournalProcessorFR_AnswersTheGroupJournalsAndTheStatus(JournalTypeFR journalTypeFR, string mediaType)
    {
        var (contentType, _) = Sut().ProcessAsync(new JournalRequest { ftJournalType = journalTypeFR.As<JournalType>() });

        contentType.MediaType.Should().Be(mediaType);
    }

    [Fact]
    public void JournalProcessorFR_RejectsANegativeTo()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = JournalTypeFR.Ticket.As<JournalType>(), To = -10 });

        act.Should().Throw<ArgumentException>().WithMessage("*negative To*");
    }

    private static JournalProcessorFR Sut() => new(Guid.NewGuid(), Mock.Of<IStorageProvider>());

    [Theory]
    [InlineData(0x4652_2000_0000_0005)]
    [InlineData(0x4652_2000_0000_0006)]
    [InlineData(0x4652_2000_0000_000C)]
    [InlineData(0x4652_2000_0000_0011)]
    public void JournalProcessorFR_RejectsUnknownFrenchJournalCodes(long ftJournalType)
    {
        var sut = Sut();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) ftJournalType });

        act.Should().Throw<ArgumentException>().WithMessage("*unknown*");
    }

    [Fact]
    public void JournalProcessorFR_RejectsAnotherCountrysJournalType()
    {
        var sut = Sut();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) 0x4154_0000_0000_0001 });

        act.Should().Throw<ArgumentException>().WithMessage("*not a French journal type*");
    }
}
