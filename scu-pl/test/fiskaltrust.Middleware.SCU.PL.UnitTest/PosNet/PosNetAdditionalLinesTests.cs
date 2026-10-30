using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// The general additional-lines contract (cbReceiptLines, and Receipt / cbPayItemLines of a pay item,
/// middleware#844) on a POSNET: what is read, what the register's limits reject before any frame is
/// sent, and how it lands inside the fiscal receipt.
/// </summary>
public class PosNetAdditionalLinesTests
{
    private static ReceiptRequest Sale(object? receiptCaseData = null, params object?[] payItemCaseData) => new()
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        Currency = Currency.PLN,
        cbChargeItems = [MappingFixture.Position("Woda", 2.00m)],
        cbPayItems = (payItemCaseData.Length == 0 ? [null] : payItemCaseData).Select(data => new PayItem
        {
            Description = "Mastercard",
            Amount = 2.00m / Math.Max(payItemCaseData.Length, 1),
            ftPayItemCase = (PayItemCase)0x504C_2000_0000_0004,
            Currency = Currency.PLN,
            ftPayItemCaseData = data,
        }).ToList(),
        ftReceiptCaseData = receiptCaseData,
    };

    private static IEnumerable<string> Texts(ReceiptRequest request) => PosNetAdditionalLines.Read(request).Select(l => l.Text);

    [Fact]
    public void ReceiptLinesAndSlips_AreLeftAlignedAdditionalLines_ReceiptLinesFirst()
    {
        var lines = PosNetAdditionalLines.Read(Sale(new { cbReceiptLines = new[] { "Thank you" } }, new { cbPayItemLines = new[] { "APPROVED" } }));

        lines.Select(l => l.Text).Should().Equal("Thank you", "APPROVED");
        lines.Should().OnlyContain(l => l.LineId == PosNetFooterLine.LeftAlignedLineId);
    }

    [Fact]
    public void Receipt_WinsOverCbPayItemLines()
        => Texts(Sale(null, new { Receipt = new[] { "from the pay endpoint" }, cbPayItemLines = new[] { "from the POS" } }))
            .Should().Equal("from the pay endpoint");

    [Fact]
    public void EachPayItemsSlip_FollowsInPayItemOrder()
        => Texts(Sale(null, new { cbPayItemLines = new[] { "first" } }, null, new { cbPayItemLines = new[] { "second" } }))
            .Should().Equal("first", "second");

    [Fact]
    public void AnEntryWithLineBreaks_IsSplit_AndAnEmptyEntryIsAnEmptyLine()
        => Texts(Sale(new { cbReceiptLines = new[] { "a\nb", "", "c\r\nd" } })).Should().Equal("a", "b", " ", "c", "d");

    [Fact]
    public void ALiteralAmpersand_IsDoubled_AsTheLeftAlignedLineReadsFormattingCodes()
        => Texts(Sale(new { cbReceiptLines = new[] { "A&B" } })).Should().Equal("A&&B");

    [Fact]
    public void WithoutAdditionalLines_NothingIsAdded()
        => PosNetAdditionalLines.Read(Sale(new { PL = new { printout = new { barcode = "123" } } }, new { other = 1 })).Should().BeEmpty();

    [Fact]
    public void ALineLongerThan80mmPaperTakes_IsRejectedBeforeAnyCommandIsBuilt()
    {
        var act = () => PosNetAdditionalLines.Read(Sale(null, new { cbPayItemLines = new[] { new string('x', 57) } }));

        act.Should().Throw<PLValidationException>().WithMessage("*payment slip of 'Mastercard'*has 57 characters*at most 56*");
    }

    [Theory]
    [InlineData("AUTH	123")]
    [InlineData("APPROVED")]
    public void ALineWithAControlCharacter_IsRejectedBeforeAnyCommandIsBuilt(string line)
    {
        var receiptLines = () => PosNetAdditionalLines.Read(Sale(new { cbReceiptLines = new[] { line } }));
        var slip = () => PosNetAdditionalLines.Read(Sale(null, new { cbPayItemLines = new[] { line } }));

        receiptLines.Should().Throw<PLValidationException>().WithMessage("*control character*");
        slip.Should().Throw<PLValidationException>().WithMessage("*payment slip*control character*");
    }

    [Fact]
    public void MoreAdditionalLinesThanTheRegisterPrints_AreRejected_CountingPrintoutReceiptAndSlipLines()
    {
        var request = Sale(new { cbReceiptLines = Enumerable.Repeat("r", 30).ToArray(), PL = new { printout = new { lines = new[] { new { text = "p" } } } } },
            new { cbPayItemLines = Enumerable.Repeat("s", 30).ToArray() });

        var act = () => PosNetReceiptMapper.MapSale(request, new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

        act.Should().Throw<PLValidationException>().WithMessage("*61 additional lines*at most 60*");
    }

    [Fact]
    public void TheReceipt_PrintsPrintoutLinesThenReceiptLinesThenTheSlip_BeforeClosingTheFooter()
    {
        var request = Sale(new { cbReceiptLines = new[] { "receipt" }, PL = new { printout = new { lines = new[] { new { text = "printout" } } } } },
            new { cbPayItemLines = new[] { "slip" } });

        var commands = PosNetReceiptMapper.MapSale(request, new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

        commands.SkipWhile(c => c.Mnemonic != "trend").Select(c => c.Mnemonic).Should().Equal("trend", "trftrln", "trftrln", "trftrln", "trftrend");
        commands.Where(c => c.Mnemonic == "trftrln").Select(c => c.Parameters.Single(p => p.Key == "na").Value).Should().Equal("printout", "receipt", "slip");
    }
}
