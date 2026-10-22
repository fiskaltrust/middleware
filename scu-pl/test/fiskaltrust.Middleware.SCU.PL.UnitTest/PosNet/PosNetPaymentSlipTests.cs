using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// The payment slip of a pay item (middleware#844): which lines are read from ftPayItemCaseData,
/// what the register's limits reject before any frame is sent, and the super-form commands it becomes.
/// </summary>
public class PosNetPaymentSlipTests
{
    private static ReceiptRequest Sale(params object?[] payItemCaseData) => new()
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        Currency = Currency.PLN,
        cbChargeItems = [MappingFixture.Position("Woda", 2.00m)],
        cbPayItems = payItemCaseData.Select(data => new PayItem
        {
            Description = "Mastercard",
            Amount = 2.00m,
            ftPayItemCase = (PayItemCase)0x504C_2000_0000_0004,
            Currency = Currency.PLN,
            ftPayItemCaseData = data,
        }).ToList(),
    };

    private static KeyValuePair<string, string> KV(string key, string value) => new(key, value);

    [Fact]
    public void CbPayItemLines_BecomeTheSlip()
    {
        var slips = PosNetPaymentSlip.Read(Sale(new { cbPayItemLines = new[] { "CARDHOLDER COPY", "", "APPROVED" } }));

        slips.Should().ContainSingle().Which.Lines.Should().Equal("CARDHOLDER COPY", "", "APPROVED");
    }

    [Fact]
    public void Receipt_WinsOverCbPayItemLines()
    {
        var slips = PosNetPaymentSlip.Read(Sale(new { Receipt = new[] { "from the pay endpoint" }, cbPayItemLines = new[] { "from the POS" } }));

        slips.Should().ContainSingle().Which.Lines.Should().Equal("from the pay endpoint");
    }

    [Fact]
    public void EachPayItemWithLines_GetsItsOwnSlip_InPayItemOrder()
    {
        var slips = PosNetPaymentSlip.Read(Sale(new { cbPayItemLines = new[] { "first" } }, null, new { cbPayItemLines = new[] { "second" } }));

        slips.Select(s => s.Lines[0]).Should().Equal("first", "second");
    }

    [Fact]
    public void APayItemWithoutLines_PrintsNoSlip()
        => PosNetPaymentSlip.Read(Sale(null, new { cbPayItemLines = Array.Empty<string>() }, new { other = 1 })).Should().BeEmpty();

    [Fact]
    public void ALineLongerThanTheSuperFormTakes_IsRejectedBeforeAnyCommandIsBuilt()
    {
        var act = () => PosNetPaymentSlip.Read(Sale(new { cbPayItemLines = new[] { new string('x', 57) } }));

        act.Should().Throw<PLValidationException>().WithMessage("*has 57 characters*at most 56*");
    }

    [Fact]
    public void MoreLinesThanTheRegisterPrints_AreRejected()
    {
        var act = () => PosNetPaymentSlip.Read(Sale(new { cbPayItemLines = Enumerable.Repeat("x", 61).ToArray() }));

        act.Should().Throw<PLValidationException>().WithMessage("*has 61 lines*at most 60*");
    }

    [Theory]
    [InlineData("", " ")]
    [InlineData("H&M", "H&&M")]
    [InlineData("TOTAL PLN 100.00", "TOTAL PLN 100.00")]
    public void ASlipLine_IsEscapedForTheSuperForm(string line, string expected)
        => PosNetPaymentSlip.ToFormLine(line).Should().Be(expected);

    [Fact]
    public void TheSlip_IsASuperFormPrintout()
    {
        PosNetCommands.Formstart(PosNetPaymentSlip.SuperForm, PosNetPaymentSlip.CardPaymentHeader).Parameters
            .Should().Equal(KV("fn", "200"), KV("fh", "53"));
        PosNetCommands.Formformattedline(PosNetPaymentSlip.SuperForm, "APPROVED").Parameters
            .Should().Equal(KV("s1", "APPROVED"), KV("fn", "200"));
        PosNetCommands.Formend(PosNetPaymentSlip.SuperForm).Parameters.Should().ContainSingle().Which.Should().Be(KV("fn", "200"));
    }
}
