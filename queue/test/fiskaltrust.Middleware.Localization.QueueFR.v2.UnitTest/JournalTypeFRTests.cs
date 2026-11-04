using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using FluentAssertions;
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

    [Theory]
    [MemberData(nameof(AllJournalTypesFR))]
    public void JournalProcessorFR_RefusesEveryKnownFrenchJournalAsNotImplemented(JournalTypeFR journalTypeFR)
    {
        var sut = new JournalProcessorFR();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = journalTypeFR.As<JournalType>() });

        act.Should().Throw<NotImplementedException>().WithMessage($"*({journalTypeFR})*not implemented*");
    }

    [Theory]
    [InlineData(0x4652_2000_0000_0005)]
    [InlineData(0x4652_2000_0000_0006)]
    [InlineData(0x4652_2000_0000_000C)]
    [InlineData(0x4652_2000_0000_0011)]
    public void JournalProcessorFR_RejectsUnknownFrenchJournalCodes(long ftJournalType)
    {
        var sut = new JournalProcessorFR();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) ftJournalType });

        act.Should().Throw<ArgumentException>().WithMessage("*unknown*");
    }

    [Fact]
    public void JournalProcessorFR_RejectsAnotherCountrysJournalType()
    {
        var sut = new JournalProcessorFR();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) 0x4154_0000_0000_0001 });

        act.Should().Throw<ArgumentException>().WithMessage("*not a French journal type*");
    }

    public static TheoryData<JournalTypeFR> AllJournalTypesFR()
    {
        var data = new TheoryData<JournalTypeFR>();
        foreach (var value in Enum.GetValues<JournalTypeFR>())
        {
            data.Add(value);
        }
        return data;
    }
}
