using System.Text.Json;
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
/// The receipt footer beyond the plain additional lines (middleware#845): the register's keyword
/// lines, left-aligned lines, the info lines, and the cashier and register taken from cbUser and
/// cbTerminalID — what is read, what the register's limits reject before any frame is sent, and the
/// commands it becomes. Layout measured on a POSNET (Thermal family) against a real H&amp;M PL receipt.
/// </summary>
public class PosNetPrintoutFooterTests
{
    private static ReceiptRequest Sale(object? receiptCaseData = null, object? user = null, string? terminal = null) => new()
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        Currency = Currency.PLN,
        cbChargeItems = [MappingFixture.Position("Woda", 2.00m)],
        cbPayItems = [new PayItem { Description = "Gotówka", Amount = 2.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN }],
        ftReceiptCaseData = receiptCaseData,
        cbUser = user,
        cbTerminalID = terminal!,
    };

    private static IReadOnlyList<PosNetCommand> Map(ReceiptRequest request)
        => PosNetReceiptMapper.MapSale(request, new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

    private static object Printout(object printout) => new { PL = new { printout } };

    private static KeyValuePair<string, string> KV(string key, string value) => new(key, value);

    [Fact]
    public void ALabelledLine_IsTheRegistersKeywordLine_WithTheTextAsItsValue()
    {
        var commands = Map(Sale(Printout(new { lines = new object[] { new { label = "itemCount", text = "1" }, new { label = "TransactionNumber", text = "16" } } })));

        var lines = commands.Where(c => c.Mnemonic == "trftrln").ToList();
        lines[0].Parameters.Should().Equal(KV("id", "26"), KV("na", "1"));
        lines[1].Parameters.Should().Equal(KV("id", "0"), KV("na", "16"));
    }

    [Fact]
    public void ALeftAlignedLine_IsLineType41_AndAPlainLineStays25()
    {
        var commands = Map(Sale(Printout(new { lines = new object[] { new { text = "Zwrot towaru do dnia: 2026-10-30", align = "left" }, new { text = "Dziękujemy!" } } })));

        var lines = commands.Where(c => c.Mnemonic == "trftrln").ToList();
        lines[0].Parameters.Should().Equal(KV("id", "41"), KV("na", "Zwrot towaru do dnia: 2026-10-30"));
        lines[1].Parameters.Should().Equal(KV("id", "25"), KV("na", "Dziękujemy!"));
    }

    [Fact]
    public void ALeftAlignedLine_PrintsALiteralAmpersand()
    {
        var commands = Map(Sale(Printout(new { lines = new object[] { new { text = "H&M Member", align = "left" } } })));

        commands.Single(c => c.Mnemonic == "trftrln").Parameters.Should().Equal(KV("id", "41"), KV("na", "H&&M Member"));
    }

    public static TheoryData<object, string> LinesTheRegisterCannotPrint => new()
    {
        { new { lines = new object[] { new { label = "loyaltyLevel", text = "Gold" } } }, "is not a POSNET keyword line" },
        { new { lines = new object[] { new { label = "itemCount", align = "left", text = "1" } } }, "both a label and an align" },
        { new { lines = new object[] { new { align = "left", doubleWidth = true, text = "x" } } }, "normal size only" },
        { new { lines = new object[] { new { align = "right", text = "x" } } }, "align 'right' is not supported" },
        { new { infoLines = new[] { "a", "b", "c", "d" } }, "at most 3 info lines" },
        { new { infoLines = new[] { new string('x', 41) } }, "at most 40 per line (ftrcfg ln)" },
        { new { infoLines = new[] { "two\nlines" } }, "send one entry per line" },
        { new { infoLines = new[] { "x" }, infoLinesAlign = "right" }, "infoLinesAlign 'right' is not supported" },
    };

    [Theory]
    [MemberData(nameof(LinesTheRegisterCannotPrint))]
    public void AFooterTheRegisterCannotPrint_IsRejectedBeforeAnyCommandIsBuilt(object printout, string expectedMessage)
    {
        var act = () => PosNetPrintoutReader.Read(Sale(Printout(printout)));

        act.Should().Throw<PLValidationException>().WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void InfoLines_AreCentredWhenAsked_AndALiteralAmpersandIsDoubled()
    {
        var printout = PosNetPrintoutReader.Read(Sale(Printout(new { infoLines = new[] { "Zwrot lub wymiana", "Dziękujemy & zapraszamy" }, infoLinesAlign = "center" })))!;

        printout.FormattedInfoLines.Should().Equal("&cZwrot lub wymiana", "&cDziękujemy && zapraszamy");
        printout.HasFooterConfiguration.Should().BeTrue();
    }

    [Fact]
    public void TheCashierAndTheRegister_ComeFromCbUserAndCbTerminalId_AlsoWithoutAPrintoutRequest()
    {
        var printout = PosNetPrintoutReader.Read(Sale(user: "238689", terminal: "02"));

        printout.Should().NotBeNull();
        printout!.Cashier.Should().Be("238689");
        printout.Register.Should().Be("02");
        printout.Lines.Should().BeEmpty();
        printout.HasFooterConfiguration.Should().BeTrue();
    }

    [Fact]
    public void ACbUserArrivingAsJson_IsReadAsText_AndAStructuredUserIsNoName()
    {
        PosNetPrintoutReader.Read(Sale(user: JsonDocument.Parse("\"Anna\"").RootElement))!.Cashier.Should().Be("Anna");
        PosNetPrintoutReader.Read(Sale(user: JsonDocument.Parse("63").RootElement))!.Cashier.Should().Be("63");
        PosNetPrintoutReader.Read(Sale(user: JsonDocument.Parse("""{"id": 63, "name": "Anna"}""").RootElement)).Should().BeNull();
    }

    [Fact]
    public void WithoutPrintoutCashierOrRegister_NothingIsConfigured()
        => PosNetPrintoutReader.Read(Sale()).Should().BeNull();

    /// <summary>
    /// A POSNET refuses a register number over 8 characters with error 10 instead of truncating it
    /// (measured with H&amp;M's real cbTerminalID "HM-PL0027-SSCO"). That is an integration error: it is
    /// rejected here, naming the field, before anything is sent.
    /// </summary>
    [Theory]
    [InlineData(null, "HM-PL0027-SSCO", "cbTerminalID 'HM-PL0027-SSCO' has 14 characters*at most 8*")]
    [InlineData("A cashier name longer than thirty-two", "02", "cbUser 'A cashier name longer than thirty-two' has 37 characters*at most 32*")]
    public void ACashierOrRegisterTheRegisterCannotPrint_IsAnIntegrationError(string? user, string terminal, string expectedMessage)
    {
        var act = () => PosNetPrintoutReader.Read(Sale(user: user, terminal: terminal));

        act.Should().Throw<PLValidationException>().WithMessage(expectedMessage);
    }

    [Fact]
    public void TheFooterConfiguration_CarriesCashierRegisterCodesAndInfoLines_ForTheNextPrintoutOnly()
    {
        var command = PosNetCommands.Ftrcfg("20260930004000200016", 0, "238689", "02", ["&cZwrot lub wymiana", "&cw ciągu 30 dni."]);

        command.Parameters.Should().Equal(
            KV("cc", "238689"), KV("cn", "02"), KV("ca", "0"),
            KV("bc", "20260930004000200016"), KV("bb", "0"),
            KV("ln", "&cZwrot lub wymiana\n&cw ciągu 30 dni."), KV("lb", "0"));
    }

    [Fact]
    public void TheEncoder_CarriesALineFeedAsContent_ButStillRefusesFramingCharacters()
    {
        var withLineFeed = () => PosNetFrame.Encode(new PosNetCommand("ftrcfg", [KV("ln", "a\nb")]));
        var withStx = () => PosNetFrame.Encode(new PosNetCommand("ftrcfg", [KV("ln", "a\u0002b")]));

        withLineFeed.Should().NotThrow();
        withStx.Should().Throw<PosNetProtocolException>();
    }
}
