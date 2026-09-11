using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetRateTableTests
{
    private static PosNetResponse Sfsk(string fields) => PosNetFrame.Decode(PosNetProtocolTests.EncodeResponse($"sfsk\tfsT\tcl0\trd1\tvt1\t{fields}rw2000-01-01;00:00\tnuABC 1234567890\t"));

    [Fact]
    public void Parse_ReadsTheRates_TheExemptSlot_AndSkipsInactiveSlots()
    {
        // The specification's own example: A 22, B 7, C 0, D 3, E/F not in use, G exempt.
        var table = PosNetRateTable.Parse(Sfsk("va22,00\tvb7,00\tvc0,00\tvd3,00\tve101,00\tvf101,00\tvg100,00\t"));

        table.Select(e => (e.PtuSlot, e.VatRatePercent, e.IsExempt)).Should().Equal(
            ("A", 22m, false), ("B", 7m, false), ("C", 0m, false), ("D", 3m, false), ("G", null, true));
    }

    [Fact]
    public void Parse_ReadsTheOfficePrinter_WhichHasNoExemptSlot()
    {
        var table = PosNetRateTable.Parse(Sfsk("va23,00\tvb8,00\tvc5,00\tvd0,00\tve101,00\tvf101,00\tvg101,00\t"));

        table.Select(e => e.PtuSlot).Should().Equal("A", "B", "C", "D");
        table.Should().NotContain(e => e.IsExempt);
    }

    [Fact]
    public void Parse_WithoutAnyActiveRate_Refuses()
    {
        var act = () => PosNetRateTable.Parse(Sfsk("va101,00\tvb101,00\tvc101,00\tvd101,00\tve101,00\tvf101,00\tvg101,00\t"));

        act.Should().Throw<PLValidationException>().WithMessage("*no active PTU rate*");
    }

    [Fact]
    public void Parse_WithARateThatIsNotANumber_NamesTheSlot()
    {
        var act = () => PosNetRateTable.Parse(Sfsk("vaXX\tvb8,00\t"));

        act.Should().Throw<PLValidationException>().WithMessage("*slot A*");
    }
}
