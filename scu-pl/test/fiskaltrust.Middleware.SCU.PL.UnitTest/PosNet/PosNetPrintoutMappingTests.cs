using System.Text;
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
/// The printout customization a POS sends in ftReceiptCaseData.PL.printout: what is read, what the
/// register's limits reject before any frame is sent, and how it lands in the command sequence.
/// </summary>
public class PosNetPrintoutMappingTests
{
    private static ReceiptRequest Sale(object? receiptCaseData) => new()
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        Currency = Currency.PLN,
        cbChargeItems = [MappingFixture.Position("Woda", 2.00m)],
        cbPayItems = [new PayItem { Description = "Gotówka", Amount = 2.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN }],
        ftReceiptCaseData = receiptCaseData,
    };

    private static IReadOnlyList<PosNetCommand> Map(ReceiptRequest request)
        => PosNetReceiptMapper.MapSale(request, new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

    private static object Printout(object printout) => new { PL = new { printout } };

    private static KeyValuePair<string, string> KV(string key, string value) => new(key, value);

    [Fact]
    public void AFullPrintoutRequest_IsReadWithItsDefaults()
    {
        var request = Sale(Printout(new
        {
            barcode = " 1234567890 ",
            qrCode = new { data = "https://example.test/r/1" },
            lines = new object[] { new { text = "Dziękujemy!", doubleWidth = true }, new { text = "Zapraszamy ponownie " } },
        }));

        var printout = PosNetPrintoutReader.Read(request);

        printout.Should().NotBeNull();
        printout!.Barcode.Should().Be("1234567890");
        printout.QrCode.Should().BeEquivalentTo(new PosNetQrCode("https://example.test/r/1", 2, 0, PosNetCode2dPosition.Below));
        printout.Lines.Should().Equal(new PosNetFooterLine("Dziękujemy!", true, false), new PosNetFooterLine("Zapraszamy ponownie", false, false));
        printout.HasFooterCodes.Should().BeTrue();
    }

    [Fact]
    public void ThePayloadMayArriveAsAJsonString_AndKeysAreCaseInsensitive()
    {
        var request = Sale("""{"pl": {"Printout": {"Lines": [{"TEXT": "x"}], "QRCODE": {"Data": "abc", "Position": "ABOVE", "pixelSize": 4, "errorCorrection": 3}}}}""");

        var printout = PosNetPrintoutReader.Read(request)!;

        printout.Lines.Should().ContainSingle().Which.Text.Should().Be("x");
        printout.QrCode.Should().BeEquivalentTo(new PosNetQrCode("abc", 4, 3, PosNetCode2dPosition.Above));
        printout.Barcode.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("""{"DE": {"something": 1}}""")]
    [InlineData("""{"PL": {"otherExtension": true}}""")]
    public void WithoutAPrintoutRequest_NothingIsRead(string? receiptCaseData)
    {
        PosNetPrintoutReader.Read(Sale(receiptCaseData)).Should().BeNull();
    }

    [Fact]
    public void WithoutAPrintoutRequest_TheSaleEndsWithAPlainTrend()
    {
        var commands = Map(Sale(null));

        commands[^1].Mnemonic.Should().Be("trend");
        commands[^1].Parameters.Should().NotContain(p => p.Key == "fe");
        commands.Should().NotContain(c => c.Mnemonic == "trftrln" || c.Mnemonic == "trftrend");
    }

    [Fact]
    public void AdditionalLines_CloseTheReceiptWithFe0_AndFollowItUntilTrftrend()
    {
        var commands = Map(Sale(Printout(new { lines = new object[] { new { text = "A", doubleWidth = true, doubleHeight = true }, new { text = "B" } } })));

        commands.Select(c => c.Mnemonic).Should().Equal("trinit", "trline", "trpayment", "trend", "trftrln", "trftrln", "trftrend");
        commands[3].Parameters.Should().Contain(new KeyValuePair<string, string>("fe", "0"));
        commands[4].Parameters.Should().Equal(KV("id", "25"), KV("na", "A"), KV("sw", "1"), KV("sh", "1"));
        commands[5].Parameters.Should().Equal(KV("id", "25"), KV("na", "B"));
    }

    [Fact]
    public void FooterCodesAlone_DoNotOpenTheFooter()
    {
        var commands = Map(Sale(Printout(new { barcode = "42" })));

        commands.Select(c => c.Mnemonic).Should().Equal("trinit", "trline", "trpayment", "trend");
        commands[^1].Parameters.Should().NotContain(p => p.Key == "fe");
    }

    [Fact]
    public void TheQrCodeTravelsInHexMode_AndTheFooterConfigurationPlacesIt()
    {
        var qrcode = PosNetCommands.Qrcode("https://x", 3, 1);
        var ftrcfg = PosNetCommands.Ftrcfg("ABC123", (int)PosNetCode2dPosition.Above);

        qrcode.Mnemonic.Should().Be("qrcode");
        qrcode.Parameters.Should().Equal(KV("px", "3"), KV("el", "1"), KV("hx", "1"), KV("tx", Convert.ToHexString(Encoding.UTF8.GetBytes("https://x"))));
        ftrcfg.Parameters.Should().Equal(KV("bc", "ABC123"), KV("bb", "1"));
        PosNetCommands.Ftrcfg(null, 2).Parameters.Should().ContainSingle().Which.Should().Be(KV("bb", "2"));
    }

    public static IEnumerable<object[]> RejectedPrintouts()
    {
        yield return [new { barcode = new string('1', 31) }, "*at most 30*"];
        yield return [new { barcode = "12 34" }, "*ASCII letters and digits*"];
        yield return [new { qrCode = new { data = "" } }, "*non-empty 'data'*"];
        yield return [new { qrCode = new { data = new string('x', 2001) } }, "*at most 2000*"];
        yield return [new { qrCode = new { data = "x", pixelSize = 9 } }, "*pixelSize*"];
        yield return [new { qrCode = new { data = "x", errorCorrection = 4 } }, "*errorCorrection*"];
        yield return [new { qrCode = new { data = "x", position = "left" } }, "*position 'left'*"];
        yield return [new { lines = new object[] { new { text = new string('a', 41) } } }, "*at most 40 per line*"];
        yield return [new { lines = Enumerable.Range(0, 61).Select(i => new { text = $"line {i}" }).ToArray() }, "*More than 60*"];
        yield return [new { lines = new object[] { new { text = "tab\there" } } }, "*framing character*"];
    }

    [Theory]
    [MemberData(nameof(RejectedPrintouts))]
    public void APrintoutTheRegisterCannotPrint_IsRejectedBeforeAnyCommandIsBuilt(object printout, string expectedMessage)
    {
        var act = () => PosNetPrintoutReader.Read(Sale(Printout(printout)));

        act.Should().Throw<PLValidationException>().WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("""{"PL": {"printout": "text"}}""")]
    [InlineData("""{"PL": {"printout": {"lines": "not an array"}}}""")]
    public void AMalformedPrintoutObject_IsRejected(string receiptCaseData)
    {
        var act = () => PosNetPrintoutReader.Read(Sale(receiptCaseData));

        act.Should().Throw<PLValidationException>().WithMessage("*PL.printout*");
    }
}
